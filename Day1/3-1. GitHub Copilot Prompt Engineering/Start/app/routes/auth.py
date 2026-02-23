import re
from fastapi import APIRouter
from fastapi.responses import JSONResponse
from app.schemas import RegisterRequest
from app.security import validate_password, hash_password
from app.storage import create_user, DuplicateEmailError

router = APIRouter(prefix="/auth", tags=["auth"])

def error(code: str, message: str, status_code: int):
    return JSONResponse(
        status_code=status_code,
        content={"ok": False, "error": {"code": code, "message": message}},
    )


def is_valid_email(email: str) -> bool:
    """Basic email format validation using regex"""
    pattern = r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'
    return re.match(pattern, email) is not None


@router.post("/register", status_code=201)
def register(request: RegisterRequest):
    """Register a new user"""
    # 1. Email format validation
    if not is_valid_email(request.email):
        return error(
            code="INVALID_EMAIL",
            message="Invalid email format",
            status_code=400
        )
    
    # 2. Password validation
    is_valid, error_message = validate_password(request.password)
    if not is_valid:
        return error(
            code="WEAK_PASSWORD",
            message=error_message,
            status_code=400
        )
    
    # 3. Check for duplicate email and create user
    try:
        password_hash = hash_password(request.password)
        user = create_user(request.email, password_hash)
        
        # 4. Success response - exclude password_hash
        return {
            "ok": True,
            "user": {
                "id": user["id"],
                "email": user["email"]
            }
        }
    except DuplicateEmailError:
        return error(
            code="DUPLICATE_EMAIL",
            message="Email already registered",
            status_code=409
        )
