"""데이터 처리 모듈

항목 처리 및 통계 계산 기능을 제공합니다.
"""

from typing import List, Dict, Optional, Any


class DataProcessingError(Exception):
    """데이터 처리 오류"""
    pass


def process_items(items: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    """활성 상태의 항목을 처리합니다.
    
    상태가 'active'인 항목들을 필터링하고 처리합니다.
    - name을 대문자로 변환
    - value에 10% 증가율 적용
    - timestamp 유지
    
    Args:
        items: 처리할 항목 리스트
            각 항목은 다음 형식:
            {
                'id': int,
                'name': str,
                'status': str ('active' 또는 'inactive'),
                'value': float,
                'timestamp': optional str
            }
    
    Returns:
        List[Dict]: 처리된 항목 리스트
        
    Raises:
        DataProcessingError: 필수 필드가 없거나 값이 유효하지 않으면
        
    Examples:
        >>> items = [
        ...     {'id': 1, 'name': 'item1', 'status': 'active', 'value': 100, 'timestamp': '2026-02-22'},
        ...     {'id': 2, 'name': 'item2', 'status': 'inactive', 'value': 200, 'timestamp': '2026-02-22'}
        ... ]
        >>> result = process_items(items)
        >>> result[0]['name']
        'ITEM1'
        >>> result[0]['value']
        110.0
        >>> len(result)
        1
    """
    result = []
    
    for idx, item in enumerate(items):
        try:
            if item.get('status') == 'active':
                # 필수 필드 검증
                required_fields = ['id', 'name', 'value']
                for field in required_fields:
                    if field not in item:
                        raise DataProcessingError(f"Missing required field '{field}' at index {idx}")
                
                # 타입 검증
                if not isinstance(item['value'], (int, float)):
                    raise DataProcessingError(f"Invalid value type at index {idx}: {type(item['value'])}")
                
                # 처리 로직
                processed = {
                    'id': item['id'],
                    'name': item['name'].upper(),
                    'value': item['value'] * 1.1,
                    'timestamp': item.get('timestamp')
                }
                result.append(processed)
        except (KeyError, TypeError) as e:
            raise DataProcessingError(f"Error processing item at index {idx}: {e}")
    
    return result


def calculate_statistics(data: List[Dict[str, Any]]) -> Optional[Dict[str, float]]:
    """데이터의 통계를 계산합니다.
    
    value 필드를 기반으로 합계, 평균, 개수를 계산합니다.
    
    Args:
        data: 통계를 계산할 항목 리스트
            각 항목은 'value' 필드를 포함해야 합니다.
    
    Returns:
        Optional[Dict]: 통계 정보를 담은 딕셔너리
            {
                'total': float,      # 모든 value의 합계
                'average': float,    # 평균값
                'count': int        # 항목 개수
            }
        빈 리스트가 입력되면 None을 반환합니다.
        
    Examples:
        >>> data = [
        ...     {'id': 1, 'value': 100},
        ...     {'id': 2, 'value': 200},
        ...     {'id': 3, 'value': 300}
        ... ]
        >>> stats = calculate_statistics(data)
        >>> stats['total']
        600
        >>> stats['average']
        200.0
    
    Raises:
        KeyError: 항목에 'value' 필드가 없으면
        TypeError: value가 숫자가 아니면
    """
    if not data:
        return None
    
    try:
        total = sum(d['value'] for d in data)
        count = len(data)
        average = total / count
        
        return {
            'total': total,
            'average': average,
            'count': count
        }
    except KeyError as e:
        raise KeyError(f"Missing 'value' field in data: {e}")
    except TypeError as e:
        raise TypeError(f"Invalid value type in data: {e}")
