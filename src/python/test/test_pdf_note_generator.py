from notebook_automation.tools.notes.note_markdown_generator import create_or_update_markdown_note_for_pdf

def test_pdf_note_generation():
    # Test paths
    pdf_path = "d:/repos/mba-notebook-automation/tests/data/sample.pdf"
    output_path = "d:/repos/mba-notebook-automation/tests/test_output/test_note.md"
    pdf_link = "https://example.com/sample.pdf"
    
    # Sample AI-generated summary with its own structure
    test_summary = """# 🎓 Educational Summary (AI Generated)

## 🧩 Topics Covered
- Strategic Management Principles
- Competitive Analysis Frameworks
- Market Positioning Strategies

## 🔑 Key Concepts Explained
The document outlines fundamental principles of strategic management, focusing on how organizations develop and maintain competitive advantages. It introduces several key frameworks for analyzing market positions and evaluating competitive forces.

## ⭐ Important Takeaways
- Competitive advantage emerges from distinct organizational capabilities
- Market positioning requires understanding both internal and external factors
- Strategic decisions should align with organizational core competencies

## 💬 Notable Quotes
> "Strategy is about making choices, trade-offs; it's about deliberately choosing to be different." - Michael Porter

## ❓ Reflection Questions
1. How do our current capabilities align with market opportunities?
2. What strategic trade-offs have we made and why?"""

    # Create metadata for the note
    metadata = {
        "title": "Strategic Management Fundamentals",
        "course": "MBA-601",
        "program": "MBA"
    }
    
    # Generate the note
    created_path = create_or_update_markdown_note_for_pdf(
        pdf_path=pdf_path,
        output_path=output_path,
        pdf_link=pdf_link,
        summary=test_summary,
        metadata=metadata
    )
    
    print(f"\nNote created at: {created_path}")
    
    # Read and print the generated content
    with open(created_path, 'r', encoding='utf-8') as f:
        content = f.read()
        print("\nGenerated content:")
        print("=" * 80)
        print(content)
        print("=" * 80)

if __name__ == "__main__":
    test_pdf_note_generation()
