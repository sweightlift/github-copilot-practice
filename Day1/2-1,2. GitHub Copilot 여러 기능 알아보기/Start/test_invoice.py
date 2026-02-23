import pytest
from invoice import LineItem, calculate_total


def test_calculate_total_basic_with_discount_and_tax():
    items = [
        LineItem(sku="A", unit_price=1000, quantity=2),  # 2000
        LineItem(sku="B", unit_price=500, quantity=1),   # 500
    ]
    # subtotal=2500, discount=300 => base=2200, tax=10% => 2420
    assert calculate_total(items, tax_rate=0.10, discount_krw=300) == 2420


def test_calculate_total_rounding():
    items = [LineItem(sku="A", unit_price=100, quantity=1)]  # subtotal=100
    # base=100, tax=0.055 => 105.5 => round to 106
    assert calculate_total(items, tax_rate=0.055, discount_krw=0) == 106


def test_calculate_total_discount_clamps_to_zero():
    items = [LineItem(sku="A", unit_price=1000, quantity=1)]  # subtotal=1000
    # discount exceeds subtotal => base=0 => total=0
    assert calculate_total(items, tax_rate=0.2, discount_krw=1500) == 0


def test_calculate_total_tax_rate_bounds():
    items = [LineItem(sku="A", unit_price=1000, quantity=1)]  # subtotal=1000
    assert calculate_total(items, tax_rate=0.0, discount_krw=0) == 1000
    assert calculate_total(items, tax_rate=1.0, discount_krw=0) == 2000


@pytest.mark.parametrize("tax_rate", [-0.01, 1.01, 2.0])
def test_calculate_total_invalid_tax_rate_raises(tax_rate):
    items = [LineItem(sku="A", unit_price=1000, quantity=1)]
    with pytest.raises(ValueError, match="tax_rate must be between 0.0 and 1.0"):
        calculate_total(items, tax_rate=tax_rate, discount_krw=0)


def test_calculate_total_negative_discount_raises():
    items = [LineItem(sku="A", unit_price=1000, quantity=1)]
    with pytest.raises(ValueError, match="discount_krw must be >= 0"):
        calculate_total(items, tax_rate=0.1, discount_krw=-1)


def test_calculate_total_empty_items():
    assert calculate_total([], tax_rate=0.2, discount_krw=0) == 0


def test_calculate_total_no_discount():
    items = [LineItem(sku="A", unit_price=1000, quantity=3)]  # subtotal=3000
    # base=3000, tax=15% => 3450
    assert calculate_total(items, tax_rate=0.15, discount_krw=0) == 3450


def test_calculate_total_exact_discount_amount():
    items = [LineItem(sku="A", unit_price=500, quantity=2)]  # subtotal=1000
    # discount equals subtotal => base=0 => total=0
    assert calculate_total(items, tax_rate=0.1, discount_krw=1000) == 0
