"""FastAPI Todo application"""

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from typing import List
from todo_service import get_todo_service

app = FastAPI(title="Todo API")

# Schemas
class TodoCreateRequest(BaseModel):
    """Request to create a todo"""
    title: str = Field(..., min_length=1, max_length=200)


class TodoResponse(BaseModel):
    """Response for a todo"""
    id: int
    title: str
    done: bool
    created_at: str


class TodoListResponse(BaseModel):
    """Response for list of todos"""
    todos: List[TodoResponse]
    total: int


# Service instance
service = get_todo_service()


@app.get("/health")
async def health():
    """Health check endpoint"""
    return {"ok": True, "status": "healthy"}


@app.post("/todos", status_code=201, response_model=TodoResponse)
async def create_todo(request: TodoCreateRequest):
    """Create a new todo
    
    Args:
        request: TodoCreateRequest with title
    
    Returns:
        TodoResponse with created todo details
    """
    try:
        todo = service.add_todo(request.title)
        return TodoResponse(
            id=todo.id,
            title=todo.title,
            done=todo.done,
            created_at=todo.created_at,
        )
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))


@app.get("/todos", response_model=TodoListResponse)
async def list_todos():
    """Get all todos
    
    Returns:
        TodoListResponse with all todos
    """
    todos = service.get_todos()
    return TodoListResponse(
        todos=[
            TodoResponse(
                id=t.id,
                title=t.title,
                done=t.done,
                created_at=t.created_at,
            )
            for t in todos
        ],
        total=len(todos),
    )


@app.get("/todos/{todo_id}", response_model=TodoResponse)
async def get_todo(todo_id: int):
    """Get a specific todo
    
    Args:
        todo_id: Todo ID
    
    Returns:
        TodoResponse with todo details
    
    Raises:
        HTTPException: If todo not found
    """
    todo = service.get_todo(todo_id)
    if not todo:
        raise HTTPException(status_code=404, detail="Todo not found")
    
    return TodoResponse(
        id=todo.id,
        title=todo.title,
        done=todo.done,
        created_at=todo.created_at,
    )


@app.put("/todos/{todo_id}/toggle", response_model=TodoResponse)
async def toggle_todo(todo_id: int):
    """Toggle the done status of a todo
    
    Args:
        todo_id: Todo ID
    
    Returns:
        TodoResponse with updated todo
    
    Raises:
        HTTPException: If todo not found
    """
    todo = service.toggle_done(todo_id)
    if not todo:
        raise HTTPException(status_code=404, detail="Todo not found")
    
    return TodoResponse(
        id=todo.id,
        title=todo.title,
        done=todo.done,
        created_at=todo.created_at,
    )


@app.delete("/todos/{todo_id}", status_code=204)
async def delete_todo(todo_id: int):
    """Delete a todo
    
    Args:
        todo_id: Todo ID
    
    Raises:
        HTTPException: If todo not found
    """
    success = service.delete_todo(todo_id)
    if not success:
        raise HTTPException(status_code=404, detail="Todo not found")
