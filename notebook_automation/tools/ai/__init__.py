"""
AI Tools

This module provides artificial intelligence and machine learning utilities:
- Text summarization using OpenAI
- Prompt generation and management
- Text chunking for large documents
"""

from notebook_automation.tools.ai.summarizer import generate_summary_with_openai
from notebook_automation.tools.ai.prompt_utils import (
    format_final_user_prompt_for_pdf,
    format_chuncked_user_prompt_for_pdf
)