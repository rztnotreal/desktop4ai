from setuptools import setup, find_packages

setup(
    name="desktop4ai",
    version="1.0.0",
    description="The Universal Sub-Token Computer-Use Protocol & Engine for AI Agents",
    long_description=open("README.md", encoding="utf-8").read() if os.path.exists("README.md") else "",
    long_description_content_type="text/markdown",
    author="desktop4ai Open Source Community",
    packages=find_packages(),
    classifiers=[
        "Programming Language :: Python :: 3",
        "Operating System :: Microsoft :: Windows",
        "Topic :: Scientific/Engineering :: Artificial Intelligence",
    ],
    python_requires=">=3.8",
)
