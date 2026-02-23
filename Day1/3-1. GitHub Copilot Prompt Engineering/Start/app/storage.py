"""
[Context for Copilot]
- DB 없이 dict 기반 저장소
- create_user(email, password_hash) -> dict{id, email, password_hash}
- get_user_by_email(email) -> dict | None
- 중복 email이면 예외(또는 False)로 처리할 수 있게 설계
"""

class DuplicateEmailError(Exception):
    """Raised when attempting to create a user with an existing email"""
    pass


# In-memory user storage
_users_db = {}
_next_id = 1


def get_user_by_email(email: str) -> dict | None:
    """
    Retrieve a user by email.
    
    Args:
        email: User's email address
    
    Returns:
        User dict with id, email, password_hash or None if not found
    """
    return _users_db.get(email)


def create_user(email: str, password_hash: str) -> dict:
    """
    Create a new user with hashed password.
    
    Args:
        email: User's email address
        password_hash: Bcrypt hashed password
    
    Returns:
        User dict with id, email, password_hash
    
    Raises:
        DuplicateEmailError: If email already exists
    """
    global _next_id
    
    if email in _users_db:
        raise DuplicateEmailError(f"Email {email} already exists")
    
    user = {
        "id": _next_id,
        "email": email,
        "password_hash": password_hash
    }
    
    _users_db[email] = user
    _next_id += 1
    
    return user
