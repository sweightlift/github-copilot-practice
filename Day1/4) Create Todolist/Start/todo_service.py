"""Todo service - core business logic"""

from typing import Dict, List, Optional
from datetime import datetime


class TodoItem:
    """Represents a single todo item"""
    
    def __init__(self, id: int, title: str, done: bool = False, created_at: str = None):
        self.id = id
        self.title = title
        self.done = done
        self.created_at = created_at or datetime.now().isoformat()
    
    def to_dict(self) -> dict:
        """Convert to dictionary"""
        return {
            "id": self.id,
            "title": self.title,
            "done": self.done,
            "created_at": self.created_at,
        }


class TodoService:
    """Service to manage todos"""
    
    def __init__(self):
        self._todos: Dict[int, TodoItem] = {}
        self._next_id = 1
    
    def add_todo(self, title: str) -> TodoItem:
        """Add a new todo
        
        Args:
            title: Todo title (must not be empty)
        
        Returns:
            Created TodoItem
        
        Raises:
            ValueError: If title is empty
        """
        if not title or not title.strip():
            raise ValueError("Todo title cannot be empty")
        
        todo = TodoItem(id=self._next_id, title=title.strip())
        self._todos[self._next_id] = todo
        self._next_id += 1
        return todo
    
    def get_todos(self) -> List[TodoItem]:
        """Get all todos
        
        Returns:
            List of TodoItems sorted by id
        """
        return sorted(self._todos.values(), key=lambda t: t.id)
    
    def get_todo(self, todo_id: int) -> Optional[TodoItem]:
        """Get a single todo by id
        
        Args:
            todo_id: Todo id to retrieve
        
        Returns:
            TodoItem if found, None otherwise
        """
        return self._todos.get(todo_id)
    
    def toggle_done(self, todo_id: int) -> Optional[TodoItem]:
        """Toggle the done status of a todo
        
        Args:
            todo_id: Todo id to toggle
        
        Returns:
            Updated TodoItem if found, None otherwise
        """
        todo = self._todos.get(todo_id)
        if todo:
            todo.done = not todo.done
        return todo
    
    def delete_todo(self, todo_id: int) -> bool:
        """Delete a todo
        
        Args:
            todo_id: Todo id to delete
        
        Returns:
            True if deleted, False if not found
        """
        if todo_id in self._todos:
            del self._todos[todo_id]
            return True
        return False
    
    def clear_all(self):
        """Clear all todos (for testing)"""
        self._todos.clear()
        self._next_id = 1


# Global todo service instance
_todo_service = TodoService()


def get_todo_service() -> TodoService:
    """Get the global todo service instance"""
    return _todo_service
