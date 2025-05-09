# MBA Program and Course Reference

This document serves as a reference for course codes and program information used in the tag structure.

## Program Information

| Program | Description |
|---------|-------------|
| program/imba | Illinois iMBA Online Program |
| program/emba | Executive MBA Program |
| program/certificate | Certificate Programs |

## Course Code Reference

| Course Code | Course Name | Subject Area | Subcategory |
|------------|-------------|--------------|------------|
| ACCY501 | Accounting for Managers | accounting | financial |
| ACCY502 | Managerial Accounting | accounting | managerial |
| ACCY503 | Accounting Analysis | accounting | audit |
| ACCY504 | Advanced Accounting | accounting | tax |
| FIN501 | Corporate Finance | finance | corporate-finance |
| FIN571 | Investments | finance | investments |
| FIN580 | Financial Valuation | finance | valuation |
| MKTG571 | Marketing Management | marketing | strategy |
| MKTG572 | Digital Marketing | marketing | digital |
| MKTG573 | Consumer Behavior | marketing | consumer-behavior |
| MKTG578 | Marketing Analytics | marketing | analytics |
| BADM508 | Leadership and Teams | leadership | teams |
| BADM509 | Managing Organizations | strategy | corporate |
| BADM520 | Strategic Management | strategy | competitive |
| BADM544 | Global Strategy | strategy | global |
| BADM567 | Project Management | operations | project-management |
| BADM566 | Supply Chain Management | operations | supply-chain |
| BADM589 | Quality Management | operations | quality-management |
| ECON528 | Statistics for Managers | economics | statistics |
| ECON540 | Managerial Economics | economics | managerial |

## Term Reference

Format: `term/YYYY-season`

| Term Code | Description |
|-----------|-------------|
| term/2023-fall | Fall Semester 2023 |
| term/2024-spring | Spring Semester 2024 |
| term/2024-summer | Summer Semester 2024 |
| term/2024-fall | Fall Semester 2024 |
| term/2025-spring | Spring Semester 2025 |

## Tag Structure Example

For a Corporate Finance lecture note:
```yaml
tags:
- type/note/lecture
- program/imba
- course/FIN501
- term/2024-spring
- mba/course/finance/corporate-finance
- status/active
```

For a Marketing case study assignment:
```yaml
tags:
- type/assignment/case-study
- program/imba
- course/MKTG571
- term/2024-fall
- mba/course/marketing/strategy
- status/active
- priority/high
```

## Automation Note

The `restructure_tags.py` script will automatically detect and apply these tags based on:
1. Course codes found in the document or path
2. Term/semester references in the content or path
3. Document type based on content and location