"""사용자 관리 모듈

UserManager 클래스를 통해 사용자 추가, 조회, 제거 등의 작업을 수행합니다.
"""

import re
from typing import Optional, List


class ValidationError(Exception):
    """사용자 검증 오류"""
    pass


class DuplicateUserError(Exception):
    """중복 사용자 오류"""
    pass


class User:
    """사용자 정보를 나타내는 클래스
    
    Attributes:
        username (str): 사용자 ID (3-20자, 알파벳 및 숫자만)
        email (str): 사용자 이메일
        age (int): 사용자 나이 (18-120)
    """
    
    def __init__(self, username: str, email: str, age: int) -> None:
        """User 객체를 초기화합니다.
        
        Args:
            username: 사용자명
            email: 사용자 이메일
            age: 사용자 나이
            
        Raises:
            ValidationError: 검증 실패 시
        """
        self.username = username
        self.email = email
        self.age = age
        if not self.validate():
            raise ValidationError(f"Invalid user data: {username}, {email}, {age}")
    
    def validate(self) -> bool:
        """사용자 정보를 검증합니다.
        
        검증 규칙:
        - username: 3-20자, 알파벳과 숫자만 허용
        - email: 유효한 이메일 형식
        - age: 18-120 범위
        
        Returns:
            bool: 검증 성공하면 True, 실패하면 False
        """
        # username 검증: 3-20자, 알파벳과 숫자만
        if not re.match(r'^[a-zA-Z0-9]{3,20}$', self.username):
            return False
        
        # email 검증: 기본적인 이메일 형식
        if not re.match(r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$', self.email):
            return False
        
        # age 검증: 18-120 범위
        if not isinstance(self.age, int) or self.age < 18 or self.age > 120:
            return False
        
        return True
    
    def to_dict(self) -> dict:
        """사용자 정보를 딕셔너리로 변환합니다.
        
        Returns:
            dict: 사용자 정보를 담은 딕셔너리
        """
        return {
            "username": self.username,
            "email": self.email,
            "age": self.age
        }


class UserManager:
    """사용자 관리 클래스
    
    O(1) 조회 성능을 위해 내부적으로 dict를 사용합니다.
    """
    
    def __init__(self) -> None:
        """UserManager 객체를 초기화합니다."""
        # O(1) 조회를 위해 dict 사용
        self._users: dict[str, User] = {}
    
    def add_user(self, user: User) -> bool:
        """사용자를 추가합니다.
        
        Args:
            user: 추가할 User 객체
            
        Returns:
            bool: 추가 성공하면 True
            
        Raises:
            DuplicateUserError: username이 이미 존재하면
            ValidationError: 사용자 정보가 유효하지 않으면
        """
        if user.username in self._users:
            raise DuplicateUserError(f"User '{user.username}' already exists")
        
        if not user.validate():
            raise ValidationError(f"Invalid user data for '{user.username}'")
        
        self._users[user.username] = user
        return True
    
    def find_user(self, username: str) -> Optional[User]:
        """사용자를 username으로 찾습니다.
        
        O(1) 시간 복잡도의 빠른 조회를 제공합니다.
        
        Args:
            username: 찾을 사용자명
            
        Returns:
            User: 찾은 사용자 객체, 없으면 None
        """
        return self._users.get(username)
    
    def get_all_users(self, sort_by: str = 'username') -> List[User]:
        """모든 사용자를 조회합니다.
        
        Args:
            sort_by: 정렬 기준 ('username', 'age'), 기본값 'username'
            
        Returns:
            List[User]: 사용자 리스트
        """
        users = list(self._users.values())
        
        if sort_by == 'age':
            users.sort(key=lambda u: u.age)
        else:  # 'username' 또는 기타
            users.sort(key=lambda u: u.username)
        
        return users
    
    def remove_user(self, username: str) -> bool:
        """사용자를 제거합니다.
        
        Args:
            username: 제거할 사용자명
            
        Returns:
            bool: 제거 성공하면 True, 사용자가 없으면 False
        """
        if username in self._users:
            del self._users[username]
            return True
        return False
    
    def update_user(self, username: str, **kwargs) -> Optional[User]:
        """사용자 정보를 업데이트합니다.
        
        Args:
            username: 업데이트할 사용자명
            **kwargs: 업데이트할 필드 (email, age 등)
            
        Returns:
            User: 업데이트된 사용자 객체, 없으면 None
            
        Raises:
            ValidationError: 업데이트된 정보가 유효하지 않으면
        """
        user = self._users.get(username)
        if not user:
            return None
        
        # 업데이트 수행
        for key, value in kwargs.items():
            if hasattr(user, key):
                setattr(user, key, value)
        
        # 검증
        if not user.validate():
            raise ValidationError(f"Invalid updated data for '{username}'")
        
        return user
