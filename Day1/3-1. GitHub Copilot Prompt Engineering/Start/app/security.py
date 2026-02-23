import re
from passlib.context import CryptContext

# bcrypt context for password hashing
pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")


def validate_password(password: str) -> tuple[bool, str | None]:
    """
    Validate password meets security requirements.
    
    Returns:
        (True, None) if valid
        (False, error_message) if invalid
    """
    if len(password) < 8:
        return False, "Password must be at least 8 characters long"
    
    if not re.search(r"\d", password):
        return False, "Password must contain at least one digit"
    
    if not re.search(r"[!@#$%^&*(),.?\":{}|<>]", password):
        return False, "Password must contain at least one special character"
    
    return True, None


def hash_password(password: str) -> str:
    """
    Hash a password using bcrypt.
    
    Args:
        password: Plain text password (never logged or stored)
    
    Returns:
        Hashed password string
    """
    return pwd_context.hash(password)


def verify_password(plain: str, hashed: str) -> bool:
    """
    Verify a plain password against a hashed password.
    
    Args:
        plain: Plain text password to verify
        hashed: Hashed password to compare against
    
    Returns:
        True if passwords match, False otherwise
    """
    return pwd_context.verify(plain, hashed)
