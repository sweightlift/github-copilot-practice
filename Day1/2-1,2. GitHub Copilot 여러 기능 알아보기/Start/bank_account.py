from __future__ import annotations
from dataclasses import dataclass

class InsufficientFunds(Exception):
    pass

@dataclass(frozen=True)
class Transaction:
    kind: str   # "deposit" | "withdraw"
    amount: int
    balance_after: int

class BankAccount:
    def __init__(self, owner: str, initial_balance: int = 0) -> None:
        if initial_balance < 0:
            raise ValueError("initial_balance must be >= 0")
        self.owner = owner
        self._balance = initial_balance
        self.ledger: list[Transaction] = []

    @property
    def balance(self) -> int:
        return self._balance

    def deposit(self, amount: int) -> None:
        # TODO: amount가 0 이하이면 ValueError      
        if amount <= 0:
            raise ValueError("amount must be > 0")
        # TODO: balance 증가
        self._balance += amount
        # TODO: ledger에 Transaction(kind="deposit") 기록
        self.ledger.append(Transaction(kind="deposit", amount=amount, balance_after=self._balance))

    def withdraw(self, amount: int) -> None:
        if amount <= 0:
            raise ValueError("amount must be > 0")
        if amount > self._balance:
            raise InsufficientFunds("Insufficient funds")
        self._balance -= amount
        self.ledger.append(Transaction(kind="withdraw", amount=amount, balance_after=self._balance))

    def statement(self) -> str:
        """
        사람이 읽을 수 있는 거래 내역을 반환한다.
        예:
        deposit  +1000  balance=1000
        withdraw  -250  balance=750
        """
        lines = []
        for transaction in self.ledger:
            sign = "+" if transaction.kind == "deposit" else "-"
            lines.append(f"{transaction.kind:8} {sign}{transaction.amount:5}  balance={transaction.balance_after}")
        return "\n".join(lines)
