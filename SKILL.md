---
name: desktop-knowledge
description: Essential OS & GUI automation runbook for Windows/Desktop control, sub-token perception, native input injection, and troubleshooting.
---

# 🧠 Desktop Automation & System Internals Knowledge Base

Этот документ аккумулирует практические знания, архитектурные открытия и готовые рецепты работы с операционной системой Windows, полученные в ходе разработки и эксплуатации **desktop4ai**.

---

## 1. Изоляция оконных станций и рабочих столов (Window Stations & Desktops)

### Проблема:
В Windows фоновые процессы, агенты и сервисы часто запускаются в интерактивной оконной станции `WinSta0`, но на **изолированном рабочем столе** (например, `exebox-XXXXXXXXX`). При попытке вызвать `SetCursorPos`, `mouse_event`, `BitBlt` или `CopyFromScreen` функции либо возвращают ошибку `The handle is invalid`, либо воздействуют на пустой фиктивный рабочий стол, а не на реальный экран пользователя.

### Решение:
1. Необходимо переключить поток на рабочий стол ввода пользователя (`Default`):
   ```csharp
   IntPtr hDesktop = OpenInputDesktop(0, false, 0x01FF);
   SetThreadDesktop(hDesktop);
   ```
2. **Критическая ловушка (Error 170 / ERROR_BUSY)**:
   Если вызвать `SetThreadDesktop` внутри управляемого потока C# `.NET` (`new Thread(...)`), вызов **провалится с ошибкой 170**, потому что среда выполнения CLR инициализирует OLE/COM хуки еще до входа в тело потока.
3. **Правильный паттерн**:
   Создавать поток через нативный Win32 API `kernel32!CreateThread`. На чистом неуправляемом потоке `SetThreadDesktop` отрабатывает мгновенно и со 100% успехом:
   ```csharp
   [DllImport("kernel32.dll", SetLastError = true)]
   public static extern IntPtr CreateThread(IntPtr lpAttr, uint dwStack, ThreadProc lpStart, IntPtr lpParam, uint dwFlags, out uint lpId);
   ```

---

## 2. Аппаратная эмуляция мыши и кривые Безье

### Проблема:
Обычный вызов `SetCursorPos(x, y)` просто меняет координаты спрайта курсора. Современные приложения (Telegram, Chrome, Electron, игры с Raw Input) игнорируют `SetCursorPos`, если нет реальных аппаратных событий `WM_MOUSEMOVE`. Кроме того, мгновенная телепортация курсора пугает пользователя и часто детектируется антифрод-системами.

### Решение:
1. Вызывать **связку** из `SetCursorPos` и `mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE)`:
   ```csharp
   public static void MoveCursorHardware(int x, int y) {
       int sw = GetSystemMetrics(0); // SM_CXSCREEN
       int sh = GetSystemMetrics(1); // SM_CYSCREEN
       SetCursorPos(x, y);
       uint absX = (uint)((x * 65535) / Math.Max(1, sw));
       uint absY = (uint)((y * 65535) / Math.Max(1, sh));
       mouse_event(0x0001 | 0x8000, absX, absY, 0, UIntPtr.Zero);
   }
   ```
2. **Плавные кривые Безье (Smooth Easing)**:
   Для естественного человеческого перемещения интерполировать координаты с шагом 12 мс по кубической кривой замедления:
   $$factor = 1.0 - (1.0 - t)^3$$
   Это делает движение визуально заметным и естественным для человека на экране.
3. **Паттерн надежного клика**:
   * Плавный ход к координате `(x, y)` (~150-250 мс).
   * Пауза 40 мс (чтобы окно зарегистрировало Hover-состояние).
   * `mouse_event(MOUSEEVENTF_LEFTDOWN, ...)`.
   * Пауза 25-30 мс.
   * `mouse_event(MOUSEEVENTF_LEFTUP, ...)`.

---

## 3. Ввод текста: Юникод без зависимости от раскладки

### Проблема:
1. `SendKeys.SendWait()` в Windows зависит от текущей раскладки клавиатуры. Если у пользователя активен английский язык, при попытке отправить русские буквы («Привет») отправляются знаки вопроса (`??????`) либо некорректные символы.
2. Вставка через буфер обмена (`Ctrl+V`) может не сработать, если сочетание клавиш заблокировано в поле или раскладка перехватывает `V` как русскую `М`.

### Решение:
Использовать `SendInput` с флагом `KEYEVENTF_UNICODE (0x0004)`.
* `wVk = 0`
* `wScan = (ushort)character`
* `dwFlags = KEYEVENTF_UNICODE`

```csharp
INPUT[] inputs = new INPUT[2];
inputs[0].type = INPUT_KEYBOARD;
inputs[0].ki.wVk = 0;
inputs[0].ki.wScan = (ushort)c;
inputs[0].ki.dwFlags = KEYEVENTF_UNICODE;

inputs[1].type = INPUT_KEYBOARD;
inputs[1].ki.wVk = 0;
inputs[1].ki.wScan = (ushort)c;
inputs[1].ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;

SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
Thread.Sleep(20);
```
**Результат**: Любые символы (русский, английский, иероглифы, эмодзи) печатаются с гарантией 100% независимо от активной раскладки.

---

## 4. Фокусировка окон и обход ограничений UIPI

### Проблема:
Начиная с Windows Vista / 10, операционная система блокирует вызов `SetForegroundWindow` из фонового процесса для предотвращения кражи фокуса (Focus Stealing Prevention).

### Решение:
1. Если приложение свернуто в трей (как Telegram): запуск исполняемого файла `Telegram.exe` мгновенно разворачивает его главное окно.
2. Для принудительной активации окна использовать каскад:
   ```csharp
   ShowWindow(hWnd, 9); // SW_RESTORE
   SwitchToThisWindow(hWnd, true);
   BringWindowToTop(hWnd);
   SetForegroundWindow(hWnd);
   ```
3. Самый надежный физический триггер фокуса: сделать физический клик мышью в неактивную область или заголовок окна `click(x, y)`.

---

## 5. Sub-Token Perception: Экономия 90% токенов

### Концепция:
Традиционный подход отправляет полноэкранные скриншоты (1024x768 / 1920x1080) на каждом шаге. Это сжигает 1800–2500 токенов на каждое действие.

### Наш стандарт (desktop4ai):
1. **Tier 1 (Semantic A11y Tree)**:
   * Сканировать UIAutomation дерево активного окна.
   * Фильтровать элементы вне видимой области окна: `BoundingRectangle.IntersectsWith(winRect)`.
   * Формировать структурированный список интерактивных контролов (Button, Edit, ListItem, TabItem).
   * Назначать компактные числовые идентификаторы `[#id] type "label" -> center (x, y)`.
   * **Расход**: ~150-300 токенов (вместо 2500).
   * **Точность**: 100% клик в центр ID элемента, ноль галлюцинаций координат.
2. **Tier 2 (Set-of-Marks)**:
   * Для нестандартных Canvas/игр отрисовывать цветные бейджи `[1]`, `[2]` прямо на скриншоте. Модель кликает по номеру метки.
3. **Tier 3 (RoI Crop)**:
   * Кропать только активное окно, а не весь 4K монитор.

---

## 6. Генерация Word `.docx` документов без установленного Office

### Открытие:
Файл `.docx` — это стандартный OpenXML ZIP-контейнер, состоящий из:
* `[Content_Types].xml`
* `_rels/.rels`
* `word/document.xml`

Его можно сгенерировать за миллисекунды через встроенный в .NET `System.IO.Compression.ZipArchive` без внешних зависимостей (`npm`, `pip`, COM `Word.Application`). Открывается в Microsoft Word, WordPad, LibreOffice, Google Docs с идеальным форматированием.

---

## 7. Человекоподобное выполнение действий (Human-Like Execution)

### Принцип отсутствия «фоновых зависаний»:
Действия должны выполняться быстро, синхронно и естественно, без создания бесконечных фоновых тасков (`manage_task`).

### Создание файла на Рабочем столе через контекстное меню (как человек):
1. Показать рабочий стол: `hotkey "win+d"`.
2. Наведение мыши на свободную область (`click(600, 500, "right")`) $\to$ открытие контекстного меню `PopupWindowSiteBridge`.
3. Плавный ход на пункт **«Создать»** `(800, 659)` и клик ЛКМ $\to$ открытие подменю.
4. Плавный ход на подпункт **«Текстовый документ»** `(1133, 528)` и клик ЛКМ $\to$ Windows создаёт файл и выделяет имя.
5. Задержка 400 мс $\to$ ввод имени через `KEYEVENTF_UNICODE` $\to$ нажатие `Enter`.

### Перетаскивание файлов (Drag & Drop):
1. Получить координаты иконки через десктопный `SysListView32` (`Progman` $\to$ `SHELLDLL_DefView` $\to$ `FolderView`).
2. Плавный ход курсора в центр иконки `(x1, y1)`.
3. Зажатие ЛКМ: `mouse_event(MOUSEEVENTF_LEFTDOWN)`.
4. Пауза 50 мс для преодоления порога перетаскивания (Drag Threshold).
5. Плавная интерполяция по Безье до целевой точки `(x2, y2)`.
6. Отпускание ЛКМ: `mouse_event(MOUSEEVENTF_LEFTUP)`.

---

## 8. Чеклист перед выполнением действий GUI

* [ ] Проверить DPI: вызывать `SetProcessDPIAware()`, чтобы экранные координаты совпадали с физическими пикселями на 125%/150% масштабах.
* [ ] Проверить, активно ли целевое окно перед вводом текста.
* [ ] Если окно в трее — запустить процесс приложения для его поднятия.
* [ ] Для клика по кнопке всегда использовать координаты центра `centerX = x + width/2`, `centerY = y + height/2`.
* [ ] При вводе использовать `KEYEVENTF_UNICODE`.
