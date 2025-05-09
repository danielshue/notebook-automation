#!/usr/bin/env python3
"""
Setup script for MBA Notebook Automation package.

This package contains tools for automating the management of MBA course notes in Obsidian,
including tag management, PDF processing, video processing, and more.
"""

from setuptools import setup, find_packages

setup(
    name="mba_notebook_automation",
    version="0.1.0",
    description="Tools for automating MBA notebook management in Obsidian",
    author="MBA Notebook Automation Team",
    packages=find_packages(include=["mba_notebook_automation", "mba_notebook_automation.*"]),
    include_package_data=True,
    python_requires=">=3.8",
    install_requires=[
        "requests",
        "ruamel.yaml",
        "pymsteams",
        "msal",
        "python-dotenv",
        "beautifulsoup4",
        "tqdm",
        "retry",
        "loguru",
        "python-docx",
    ],
    extras_require={
        "dev": [
            "pytest",
            "pytest-cov",
            "black",
            "isort",
            "flake8",
            "mypy",
            "pylint",
        ]
    },    entry_points={
        "console_scripts": [
            "mba-configure=mba_notebook_automation.configure:main",
            "mba-add-nested-tags=mba_notebook_automation.tags.add_nested_tags:main",
            "mba-generate-pdf-notes=mba_notebook_automation.generate_pdf_notes_from_onedrive:main",
            "mba-generate-video-metadata=mba_notebook_automation.generate_video_meta_from_onedrive:main",
        ],
    },
)
