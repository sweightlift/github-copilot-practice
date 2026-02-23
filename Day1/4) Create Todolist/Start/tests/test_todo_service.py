"""Tests for TodoService"""

import pytest
from todo_service import TodoService


@pytest.fixture
def service():
    """Create a fresh TodoService for each test"""
    service = TodoService()
    yield service


def test_add_todo_creates_todo(service):
    """Test adding a todo"""
    todo = service.add_todo("Buy groceries")
    assert todo.id == 1
    assert todo.title == "Buy groceries"
    assert todo.done is False


def test_add_todo_auto_increments_id(service):
    """Test that IDs auto-increment"""
    todo1 = service.add_todo("Task 1")
    todo2 = service.add_todo("Task 2")
    assert todo1.id == 1
    assert todo2.id == 2


def test_add_todo_empty_title_raises_error(service):
    """Test that empty title raises ValueError"""
    with pytest.raises(ValueError, match="cannot be empty"):
        service.add_todo("")
    
    with pytest.raises(ValueError, match="cannot be empty"):
        service.add_todo("   ")


def test_add_todo_strips_whitespace(service):
    """Test that whitespace is stripped"""
    todo = service.add_todo("  Clean room  ")
    assert todo.title == "Clean room"


def test_get_todos_returns_all(service):
    """Test getting all todos"""
    service.add_todo("Task 1")
    service.add_todo("Task 2")
    service.add_todo("Task 3")
    
    todos = service.get_todos()
    assert len(todos) == 3
    assert todos[0].title == "Task 1"
    assert todos[1].title == "Task 2"
    assert todos[2].title == "Task 3"


def test_get_todos_empty_list(service):
    """Test getting todos when none exist"""
    todos = service.get_todos()
    assert todos == []


def test_get_todo_by_id(service):
    """Test getting a specific todo"""
    service.add_todo("Task 1")
    service.add_todo("Task 2")
    
    todo = service.get_todo(1)
    assert todo is not None
    assert todo.title == "Task 1"


def test_get_todo_not_found(service):
    """Test getting a non-existent todo"""
    todo = service.get_todo(999)
    assert todo is None


def test_toggle_done_changes_status(service):
    """Test toggling done status"""
    todo = service.add_todo("Task")
    assert todo.done is False
    
    updated = service.toggle_done(1)
    assert updated.done is True
    
    updated = service.toggle_done(1)
    assert updated.done is False


def test_toggle_done_not_found(service):
    """Test toggling non-existent todo"""
    result = service.toggle_done(999)
    assert result is None


def test_delete_todo_removes_todo(service):
    """Test deleting a todo"""
    service.add_todo("Task 1")
    service.add_todo("Task 2")
    
    success = service.delete_todo(1)
    assert success is True
    assert service.get_todo(1) is None
    assert len(service.get_todos()) == 1


def test_delete_todo_not_found(service):
    """Test deleting non-existent todo"""
    success = service.delete_todo(999)
    assert success is False


def test_to_dict(service):
    """Test converting todo to dict"""
    todo = service.add_todo("Task")
    todo_dict = todo.to_dict()
    
    assert todo_dict["id"] == 1
    assert todo_dict["title"] == "Task"
    assert todo_dict["done"] is False
    assert "created_at" in todo_dict


def test_clear_all_removes_todos(service):
    """Test clearing all todos"""
    service.add_todo("Task 1")
    service.add_todo("Task 2")
    assert len(service.get_todos()) == 2
    
    service.clear_all()
    assert len(service.get_todos()) == 0
