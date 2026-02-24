# Vibe Coding Project Proposal
## Expense Tracker with Statistics Dashboard

---

# 1. Project Overview

## Project Title
Smart Expense Tracker with Analytics API

## Goal

Build a simple but practically usable web application that allows users to:

- Record daily expenses
- Categorize expenses
- View monthly statistics
- Analyze spending patterns

The project will fully utilize GitHub Copilot during development.

---

# 2. Why This Project?

Many people struggle to track and analyze their spending habits.

This application provides:

- Clear expense management
- Category-based aggregation
- Monthly summaries
- Data-driven spending insights

It is simple, practical, and realistically usable.

---

# 3. Required Evaluation Components

## 3.1 Core Logic

The core logic will include:

- Category-based expense aggregation
- Monthly total calculation
- Average daily spending calculation
- Highest spending category detection

### Example Core Logic

- `getTotalByMonth(month)`
- `getTotalByCategory(category)`
- `getHighestSpendingCategory()`
- `getDailyAverage(month)`

---

## 3.2 API Design

### POST /expenses

Request:
```json
{
  "amount": 25.50,
  "category": "Food",
  "date": "2026-02-20"
}
````

Response:

```json
{
  "message": "Expense added successfully"
}
```

---

### GET /expenses

Response:

```json
[
  {
    "id": 1,
    "amount": 25.50,
    "category": "Food",
    "date": "2026-02-20"
  }
]
```

---

### GET /stats/monthly?month=2026-02

Response:

```json
{
  "total": 820.75,
  "dailyAverage": 29.31,
  "highestCategory": "Food"
}
```

---

# 4. UI Requirements

The UI will include:

* Expense input form
* Category dropdown
* Expense list table
* Monthly statistics dashboard
* Basic charts (e.g., category breakdown)

The UI can be implemented using:

* Simple HTML/CSS + JavaScript
  OR
* React (if preferred)

---

# 5. Testing Strategy

Unit tests will be written for:

* Expense addition logic
* Monthly aggregation logic
* Category total calculation
* Highest category detection

Example test cases:

* Should correctly calculate total for a given month
* Should return correct highest spending category
* Should handle empty data cases
* Should handle invalid inputs

Testing tools:

* Jest (JavaScript)
  OR
* Pytest (Python)

---

# 6. Tech Stack Options

Option A (JavaScript Stack)

* Node.js
* Express
* Jest
* React (optional frontend)

Option B (Python Stack)

* Flask or FastAPI
* Pytest
* Simple HTML templates or frontend framework

---

# 7. GitHub Copilot Usage Documentation

This section will be included in the README:

## GitHub Copilot Usage

During development, GitHub Copilot was used for:

* Generating API skeletons
* Writing initial core logic functions
* Generating unit test templates
* Suggesting refactoring improvements
* Assisting with UI event handlers
* Improving code documentation

All generated code was reviewed, tested, and refined manually.

---

# 8. Deliverables Checklist

* Public GitHub repository
* Complete README

  * Project description
  * Installation instructions
  * API documentation
  * Core logic explanation
* Unit test files included
* Running application
* Screenshots of execution
* Clear commit history

---

# 9. Why This Project Can Receive Grade A (30/30)

* Clearly defined goal and usefulness
* Distinct and testable core logic
* Structured API design
* Working UI
* Complete test coverage
* Executable application
* Documented GitHub Copilot usage

---

# Conclusion

The Smart Expense Tracker is:

* Practical
* Structured
* Testable
* API-driven
* UI-complete
* Suitable for demonstrating GitHub Copilot usage
