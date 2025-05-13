#!/usr/bin/env python3
"""
Setup script for MBA Notebook Automation package.

This package contains tools for automating the management of MBA course notes in Obsidian,
including tag management, PDF processing, video processing, and more.
"""

from setuptools import setup, find_packages

setup(    name="notebook_automation",
    version="0.1.0",
    description="Tools for automating notebook management in Obsidian",
    author="Dan Shue",
    packages=find_packages(include=["notebook_automation", "notebook_automation.*"]),
    include_package_data=True,
    python_requires=">=3.8",    install_requires=[
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
        "colorlog",
        "html2text>=2024.2.26",  # Required for markdown conversion
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
            "vault-configure=notebook_automation.cli.configure_tool:main",
            "vault-add-nested-tags=notebook_automation.cli.add_nested_tags:main",
            "vault-add-example-tags=notebook_automation.cli.add_example_tags:main",
            "vault-generate-pdf-notes=notebook_automation.cli.generate_pdf_notes:main",
            "vault-clean-index-tags=notebook_automation.cli.clean_index_tags:main",
            "vault-consolidate-tags=notebook_automation.cli.consolidate_tags:main",
            "vault-generate-tag-doc=notebook_automation.cli.generate_tag_doc:main",
            "vault-restructure-tags=notebook_automation.cli.restructure_tags:main",
            "vault-tag-manager=notebook_automation.cli.tag_manager:main",
            "vault-generate-dataview=notebook_automation.cli.generate_dataview:main",
            "vault-convert-markdown=notebook_automation.cli.convert_markdown:main",
            "vault-generate-video-meta=notebook_automation.cli.generate_video_meta:main",
            "vault-extract-pdf-pages=notebook_automation.cli.extract_pdf_pages:main",
            "vault-list-folder=notebook_automation.cli.list_folder_contents:main",
            "vault-generate-index=notebook_automation.cli.generate_vault_index:main",
            "vault-ensure-metadata=notebook_automation.cli.ensure_metadata:main",
            "vault-generate-templates=notebook_automation.cli.generate_templates:main",
            "vault-generate-markdown=notebook_automation.cli.generate_markdown:main",
            "vault-onedrive-share=notebook_automation.cli.onedrive_share:main",
            "vault-create-class-dashboards=notebook_automation.cli.create_class_dashboards:main",
        ],
    },
)
