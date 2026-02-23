"""Streamlit UI for Todo application"""

import streamlit as st
import requests
import json
from datetime import datetime

# Configuration
API_BASE_URL = "http://127.0.0.1:8000"

st.set_page_config(page_title="Todo App", layout="centered")
st.title("📝 Todo Application")

# Helper functions
def get_todos():
    """Fetch todos from API"""
    try:
        response = requests.get(f"{API_BASE_URL}/todos")
        if response.status_code == 200:
            return response.json()["todos"]
    except Exception as e:
        st.error(f"Failed to fetch todos: {e}")
    return []


def create_todo(title: str):
    """Create a new todo"""
    try:
        response = requests.post(
            f"{API_BASE_URL}/todos",
            json={"title": title}
        )
        if response.status_code == 201:
            st.success("Todo created successfully!")
            return True
        else:
            st.error(f"Failed to create todo: {response.text}")
    except Exception as e:
        st.error(f"Error creating todo: {e}")
    return False


def toggle_todo(todo_id: int):
    """Toggle todo done status"""
    try:
        response = requests.put(
            f"{API_BASE_URL}/todos/{todo_id}/toggle"
        )
        if response.status_code == 200:
            return True
    except Exception as e:
        st.error(f"Error toggling todo: {e}")
    return False


def delete_todo(todo_id: int):
    """Delete a todo"""
    try:
        response = requests.delete(
            f"{API_BASE_URL}/todos/{todo_id}"
        )
        if response.status_code == 204:
            st.success("Todo deleted!")
            return True
    except Exception as e:
        st.error(f"Error deleting todo: {e}")
    return False


# Main UI
st.subheader("✨ Create a new todo")

col1, col2 = st.columns([4, 1])
with col1:
    new_todo_title = st.text_input(
        "Todo title",
        placeholder="Enter a new task...",
        key="new_todo_input"
    )

with col2:
    if st.button("Add", use_container_width=True):
        if new_todo_title.strip():
            if create_todo(new_todo_title):
                st.rerun()
        else:
            st.warning("Please enter a todo title")

st.divider()

st.subheader("📋 Your Todos")

# Fetch and display todos
todos = get_todos()

if not todos:
    st.info("No todos yet. Create one above! 🚀")
else:
    for todo in todos:
        col1, col2, col3 = st.columns([1, 3, 1])
        
        # Toggle button
        with col1:
            status = "✅" if todo["done"] else "⭕"
            if st.button(status, key=f"toggle_{todo['id']}", use_container_width=True):
                if toggle_todo(todo["id"]):
                    st.rerun()
        
        # Todo content
        with col2:
            title_style = "text-decoration: line-through" if todo["done"] else ""
            st.markdown(f"**{todo['title']}**", unsafe_allow_html=True)
            st.caption(f"Created: {todo['created_at'][:10]}")
        
        # Delete button
        with col3:
            if st.button("🗑️", key=f"delete_{todo['id']}", use_container_width=True):
                if delete_todo(todo["id"]):
                    st.rerun()

st.divider()

# Statistics
if todos:
    total = len(todos)
    completed = sum(1 for t in todos if t["done"])
    pending = total - completed
    
    col1, col2, col3 = st.columns(3)
    with col1:
        st.metric("Total", total)
    with col2:
        st.metric("Completed", completed)
    with col3:
        st.metric("Pending", pending)

# API Status
with st.expander("ℹ️ API Status"):
    try:
        response = requests.get(f"{API_BASE_URL}/health", timeout=2)
        if response.status_code == 200:
            st.success("API is running and healthy ✅")
        else:
            st.warning("API is not responding correctly")
    except Exception:
        st.error("⚠️ Cannot connect to API. Make sure FastAPI server is running on port 8000")
        st.write("Start the server with: `uvicorn main:app --reload`")
