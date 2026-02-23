from fastapi import FastAPI
from fastapi.responses import JSONResponse
from app import reviews

app = FastAPI(title="Review API")


@app.get("/health")
async def health():
    """헬스 체크"""
    return {"ok": True, "status": "healthy"}


app.include_router(reviews.router, prefix="/api")
