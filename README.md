# FinStatsWallet

Aplikacja webowa do zarządzania domowym budżetem.

## MVP

- Strona główna (podgląd)
- Rejestracja i logowanie użytkownika
- Zarządzanie saldem (wpłaty i wydatki)
- Dashboard statystyk (przychody, wydatki, bilans)
- Działająca baza danych PostgreSQL

## Model danych (PostgreSQL)

Poniżej znajduje się docelowy, znormalizowany schemat bazy danych dla aplikacji.

```sql
CREATE TABLE users (
    id              BIGSERIAL PRIMARY KEY,
    login           VARCHAR(50)   NOT NULL UNIQUE,
    email           VARCHAR(255)  NOT NULL UNIQUE,
    password_hash   VARCHAR(255)  NOT NULL,
    full_name       VARCHAR(120)  NOT NULL,
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE accounts (
    id              BIGSERIAL PRIMARY KEY,
    user_id         BIGINT        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name            VARCHAR(80)   NOT NULL,
    account_type    VARCHAR(20)   NOT NULL CHECK (account_type IN ('cash', 'bank', 'savings', 'credit')),
    currency_code   CHAR(3)       NOT NULL DEFAULT 'PLN',
    opening_balance NUMERIC(14,2) NOT NULL DEFAULT 0,
    is_active       BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, name),
    UNIQUE (id, user_id)
);

CREATE TABLE categories (
    id              BIGSERIAL     PRIMARY KEY,
    user_id         BIGINT        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name            VARCHAR(80)   NOT NULL,
    category_type     VARCHAR(10)   NOT NULL CHECK (category_type IN ('income', 'expense')),
    parent_category_id BIGINT       REFERENCES categories(id) ON DELETE SET NULL,
    is_active         BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, category_type, name),
    UNIQUE (id, user_id),
    CHECK (parent_category_id IS NULL OR parent_category_id <> id)
);

CREATE TABLE transactions (
    id              BIGSERIAL PRIMARY KEY,
    user_id         BIGINT        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    account_id      BIGINT        NOT NULL,
    category_id     BIGINT        REFERENCES categories(id) ON DELETE SET NULL,
    amount          NUMERIC(14,2) NOT NULL CHECK (amount > 0),
    direction       VARCHAR(10)   NOT NULL CHECK (direction IN ('income', 'expense')),
    note            TEXT,
    occurred_at     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    FOREIGN KEY (account_id, user_id)
        REFERENCES accounts(id, user_id)
        ON DELETE CASCADE
);

CREATE TABLE budget_periods (
    id              BIGSERIAL PRIMARY KEY,
    user_id         BIGINT        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name            VARCHAR(80)   NOT NULL,
    period_start    DATE          NOT NULL,
    period_end      DATE          NOT NULL,
    currency_code   CHAR(3)       NOT NULL DEFAULT 'PLN',
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CHECK (period_end >= period_start),
    CHECK (currency_code ~ '^[A-Z]{3}$'),
    UNIQUE (user_id, period_start, period_end),
    UNIQUE (id, user_id)
);

CREATE TABLE budget_category_limits (
    id              BIGSERIAL PRIMARY KEY,
    user_id         BIGINT        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    budget_period_id BIGINT        NOT NULL,
    category_id      BIGINT        NOT NULL,
    amount_limit      NUMERIC(14,2) NOT NULL CHECK (amount_limit >= 0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (budget_period_id, category_id, user_id),
    FOREIGN KEY (budget_period_id, user_id)
        REFERENCES budget_periods(id, user_id)
        ON DELETE CASCADE,
    FOREIGN KEY (category_id, user_id)
        REFERENCES categories(id, user_id)
        ON DELETE CASCADE
);
```

indexy
```sql
CREATE INDEX idx_accounts_user_id
    ON accounts(user_id);

CREATE INDEX idx_transactions_user_id
    ON transactions(user_id);

CREATE INDEX idx_transactions_account_occurred
    ON transactions(account_id, occurred_at DESC);

CREATE INDEX idx_transactions_category_occurred
    ON transactions(category_id, occurred_at DESC);

CREATE INDEX idx_transactions_user_direction_occurred
    ON transactions(user_id, direction, occurred_at DESC);

CREATE INDEX idx_bcl_budget_period_user
    ON budget_category_limits(budget_period_id, user_id);

CREATE INDEX idx_bcl_category_user
    ON budget_category_limits(category_id, user_id);
```

unikalności i spójność relacji:
- `transactions(account_id, user_id) -> accounts(id, user_id)`
- `budget_category_limits(budget_period_id, user_id) -> budget_periods(id, user_id)`
- `budget_category_limits(category_id, user_id) -> categories(id, user_id)`

Trigger dla transactions

sprawdza:
- czy category_id, jeśli nie jest nullem, należy do user_id,
- czy categories.category_type = transactions.direction.

```sql
CREATE OR REPLACE FUNCTION trg_transactions_validate()
RETURNS TRIGGER AS $$
DECLARE
    v_category_type VARCHAR(10);
BEGIN
    IF NEW.category_id IS NOT NULL THEN
        SELECT category_type
        INTO v_category_type
        FROM categories
        WHERE id = NEW.category_id
          AND user_id = NEW.user_id;

        IF v_category_type IS NULL THEN
            RAISE EXCEPTION 'Category % does not belong to user %', NEW.category_id, NEW.user_id;
        END IF;

        IF v_category_type <> NEW.direction THEN
            RAISE EXCEPTION 'Transaction direction (%) must match category type (%)',
                NEW.direction, v_category_type;
        END IF;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
   
-- trigger 
CREATE TRIGGER before_transactions_validate
    BEFORE INSERT OR UPDATE ON transactions
                         FOR EACH ROW
                         EXECUTE FUNCTION trg_transactions_validate();
```


Budget tylko dla wydatków
```sql
CREATE OR REPLACE FUNCTION trg_budget_category_limits_validate()
RETURNS TRIGGER AS $$
DECLARE
    v_category_type VARCHAR(10);
BEGIN
    SELECT category_type
    INTO v_category_type
    FROM categories
    WHERE id = NEW.category_id
      AND user_id = NEW.user_id;

    IF v_category_type IS NULL THEN
        RAISE EXCEPTION 'Category % does not belong to user %', NEW.category_id, NEW.user_id;
    END IF;

    IF v_category_type <> 'expense' THEN
        RAISE EXCEPTION 'Only expense categories can have budget limits';
END IF;

RETURN NEW;
END;
$$ LANGUAGE plpgsql;


CREATE TRIGGER before_budget_category_limits_validate
    BEFORE INSERT OR UPDATE ON budget_category_limits
                         FOR EACH ROW
                         EXECUTE FUNCTION trg_budget_category_limits_validate();
```

Trigger dla automatycznego updated_at
```sql
-- funkcja
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
   
-- triger
CREATE TRIGGER trg_users_set_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_accounts_set_updated_at
    BEFORE UPDATE ON accounts
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_categories_set_updated_at
    BEFORE UPDATE ON categories
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_transactions_set_updated_at
    BEFORE UPDATE ON transactions
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_budget_periods_set_updated_at
    BEFORE UPDATE ON budget_periods
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_budget_category_limits_set_updated_at
    BEFORE UPDATE ON budget_category_limits
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

-- lower login, email, name
CREATE OR REPLACE FUNCTION trg_users_normalize()
RETURNS TRIGGER AS $$
BEGIN
    NEW.login = lower(trim(NEW.login));
    NEW.email = lower(trim(NEW.email));
    NEW.full_name = trim(NEW.full_name);
RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER before_users_normalize
    BEFORE INSERT OR UPDATE ON users
                         FOR EACH ROW
                         EXECUTE FUNCTION trg_users_normalize();
```
