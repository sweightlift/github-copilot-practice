"""리뷰 관련 라우터"""

from fastapi import APIRouter, HTTPException, Query
from typing import Optional
from app.schemas import (
    ReviewCreateRequest,
    ReviewUpdateRequest,
    ReviewResponse,
    ReviewListResponse,
    RatingStats,
    SuccessResponse,
    ErrorResponse,
)
from app import storage

router = APIRouter(prefix="/reviews", tags=["reviews"])


@router.post("", status_code=201)
async def create_review(request: ReviewCreateRequest) -> SuccessResponse:
    """리뷰 작성"""
    review = storage.save_review(
        product_id=request.product_id,
        rating=request.rating,
        title=request.title,
        content=request.content,
        reviewer=request.reviewer,
    )
    
    return SuccessResponse(
        review=ReviewResponse(
            id=review["id"],
            product_id=review["product_id"],
            rating=review["rating"],
            title=review["title"],
            content=review["content"],
            reviewer=review["reviewer"],
            created_at=review["created_at"],
            updated_at=review["updated_at"],
        )
    )


@router.get("/{review_id}")
async def get_review(review_id: int) -> SuccessResponse:
    """리뷰 조회"""
    review = storage.get_review(review_id)
    if not review:
        raise HTTPException(status_code=404, detail="Review not found")
    
    return SuccessResponse(
        review=ReviewResponse(
            id=review["id"],
            product_id=review["product_id"],
            rating=review["rating"],
            title=review["title"],
            content=review["content"],
            reviewer=review["reviewer"],
            created_at=review["created_at"],
            updated_at=review["updated_at"],
        )
    )


@router.get("/products/{product_id}")
async def list_reviews_by_product(
    product_id: str,
    sort: str = Query("rating_desc", regex="^(rating_desc|rating_asc|recent)$"),
    min_rating: Optional[int] = Query(None, ge=1, le=5),
) -> ReviewListResponse:
    """상품별 리뷰 목록 조회"""
    reviews_list, stats = storage.get_reviews_by_product(
        product_id=product_id,
        sort=sort,
        min_rating=min_rating,
    )
    
    return ReviewListResponse(
        product_id=product_id,
        reviews=[
            ReviewResponse(
                id=r["id"],
                product_id=r["product_id"],
                rating=r["rating"],
                title=r["title"],
                content=r["content"],
                reviewer=r["reviewer"],
                created_at=r["created_at"],
                updated_at=r["updated_at"],
            )
            for r in reviews_list
        ],
        stats=RatingStats(
            total=stats["total"],
            average_rating=stats["average_rating"],
            rating_distribution=stats["rating_distribution"],
        ),
    )


@router.put("/{review_id}")
async def update_review(
    review_id: int,
    request: ReviewUpdateRequest,
) -> SuccessResponse:
    """리뷰 수정"""
    # 리뷰 존재 여부 확인
    review = storage.get_review(review_id)
    if not review:
        raise HTTPException(status_code=404, detail="Review not found")
    
    # 업데이트할 필드만 전달
    update_data = {}
    if request.rating is not None:
        update_data["rating"] = request.rating
    if request.title is not None:
        update_data["title"] = request.title
    if request.content is not None:
        update_data["content"] = request.content
    
    updated_review = storage.update_review(review_id, **update_data)
    
    return SuccessResponse(
        review=ReviewResponse(
            id=updated_review["id"],
            product_id=updated_review["product_id"],
            rating=updated_review["rating"],
            title=updated_review["title"],
            content=updated_review["content"],
            reviewer=updated_review["reviewer"],
            created_at=updated_review["created_at"],
            updated_at=updated_review["updated_at"],
        )
    )


@router.delete("/{review_id}", status_code=204)
async def delete_review(review_id: int):
    """리뷰 삭제"""
    success = storage.delete_review(review_id)
    if not success:
        raise HTTPException(status_code=404, detail="Review not found")
