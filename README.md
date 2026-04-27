# FinStatsWallet

Aplikacja webowa do zarządzania domowym budżetem.

## MVP

- Strona główna (podgląd)
- Rejestracja i logowanie użytkownika
- Zarządzanie saldem (wpłaty i wydatki)
- Dashboard statystyk (przychody, wydatki, bilans)
- Działająca baza danych PostgreSQL

## Konfiguracja połączenia PostgreSQL (ASP.NET)

Wrażliwe pliki `appsettings*.json` nie są śledzone przez Git.
Skopiuj:
- `appsettings.Example.json` -> `appsettings.json`,
- `appsettings.Development.Example.json` -> `appsettings.Development.json`,
i uzupełnij `ConnectionStrings:Postgres` własnymi danymi.

Przykład:
`Host=localhost;Port=5432;Database=finstatswallet;Username=your_user;Password=your_password`

## Model danych (PostgreSQL)

Below is a complete, single SQL script ready to run in pgAdmin.

```sql
-- =========================================================
-- FinStatsWallet - complete PostgreSQL schema (MVP)
-- =========================================================
-- This script creates:
-- 1) tables and relationships (with per-user_id isolation),
-- 2) indexes,
-- 3) functions + validation and updated_at triggers.
-- =========================================================

BEGIN;

-- ---------------------------------------------------------
-- USERS
-- ---------------------------------------------------------
CREATE TABLE IF NOT EXISTS users (
    id              BIGSERIAL PRIMARY KEY,
    login           VARCHAR(50)   NOT NULL UNIQUE,
    email           VARCHAR(255)  NOT NULL UNIQUE,
    password_hash   VARCHAR(255)  NOT NULL,
    full_name       VARCHAR(120)  NOT NULL,
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

-- ---------------------------------------------------------
-- ACCOUNTS
-- ---------------------------------------------------------
CREATE TABLE IF NOT EXISTS accounts (
    id              BIGSERIAL     PRIMARY KEY,
    user_id         BIGINT        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name            VARCHAR(80)   NOT NULL,
    account_type    VARCHAR(20)   NOT NULL CHECK (account_type IN ('cash', 'bank', 'savings', 'credit')),
    currency_code   CHAR(3)       NOT NULL DEFAULT 'PLN',
    opening_balance NUMERIC(14,2) NOT NULL DEFAULT 0,
    is_active       BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CHECK (currency_code ~ '^[A-Z]{3}$'),
    UNIQUE (user_id, name),
    UNIQUE (id, user_id)
);

-- ---------------------------------------------------------
-- CATEGORIES
-- ---------------------------------------------------------
CREATE TABLE IF NOT EXISTS categories (
    id                  BIGSERIAL    PRIMARY KEY,
    user_id             BIGINT       NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name                VARCHAR(80)  NOT NULL,
    category_type       VARCHAR(10)  NOT NULL CHECK (category_type IN ('income', 'expense')),
    parent_category_id  BIGINT,
    is_active           BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, category_type, name),
    UNIQUE (id, user_id),
    CHECK (parent_category_id IS NULL OR parent_category_id <> id),
    -- Null parent_category_id when parent category is deleted
    FOREIGN KEY (parent_category_id)
        REFERENCES categories(id)
        ON DELETE SET NULL,
    -- Enforce that parent category belongs to the same user
    FOREIGN KEY (parent_category_id, user_id)
        REFERENCES categories(id, user_id)
        ON DELETE NO ACTION
);

-- ---------------------------------------------------------
-- TRANSACTIONS
-- ---------------------------------------------------------
CREATE TABLE IF NOT EXISTS transactions (
    id              BIGSERIAL PRIMARY KEY,
    user_id         BIGINT        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    account_id      BIGINT        NOT NULL,
    category_id     BIGINT,
    amount          NUMERIC(14,2) NOT NULL CHECK (amount > 0),
    direction       VARCHAR(10)   NOT NULL CHECK (direction IN ('income', 'expense')),
    note            TEXT,
    occurred_at     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    -- Account must belong to the same user
    FOREIGN KEY (account_id, user_id)
        REFERENCES accounts(id, user_id)
        ON DELETE CASCADE,
    -- Null category_id when category is deleted
    FOREIGN KEY (category_id)
        REFERENCES categories(id)
        ON DELETE SET NULL,
    -- Enforce that category (if set) belongs to the same user
    FOREIGN KEY (category_id, user_id)
        REFERENCES categories(id, user_id)
        ON DELETE NO ACTION
);

-- ---------------------------------------------------------
-- BUDGET_PERIODS
-- ---------------------------------------------------------
CREATE TABLE IF NOT EXISTS budget_periods (
    id              BIGSERIAL     PRIMARY KEY,
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

-- ---------------------------------------------------------
-- BUDGET_CATEGORY_LIMITS
-- ---------------------------------------------------------
CREATE TABLE IF NOT EXISTS budget_category_limits (
    id               BIGSERIAL     PRIMARY KEY,
    user_id          BIGINT        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    budget_period_id BIGINT        NOT NULL,
    category_id      BIGINT        NOT NULL,
    amount_limit     NUMERIC(14,2) NOT NULL CHECK (amount_limit >= 0),
    created_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    UNIQUE (budget_period_id, category_id, user_id),
    -- Budget period must belong to the same user
    FOREIGN KEY (budget_period_id, user_id)
        REFERENCES budget_periods(id, user_id)
        ON DELETE CASCADE,
    -- Category must belong to the same user
    FOREIGN KEY (category_id, user_id)
        REFERENCES categories(id, user_id)
        ON DELETE CASCADE
);

-- ---------------------------------------------------------
-- INDEXES
-- ---------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_accounts_user_id
    ON accounts(user_id);

CREATE INDEX IF NOT EXISTS idx_transactions_user_id
    ON transactions(user_id);

CREATE INDEX IF NOT EXISTS idx_transactions_account_occurred
    ON transactions(account_id, occurred_at DESC);

CREATE INDEX IF NOT EXISTS idx_transactions_category_occurred
    ON transactions(category_id, occurred_at DESC);

CREATE INDEX IF NOT EXISTS idx_transactions_user_direction_occurred
    ON transactions(user_id, direction, occurred_at DESC);

CREATE INDEX IF NOT EXISTS idx_budget_category_limits_budget_period_user
    ON budget_category_limits(budget_period_id, user_id);

CREATE INDEX IF NOT EXISTS idx_budget_category_limits_category_user
    ON budget_category_limits(category_id, user_id);

-- ---------------------------------------------------------
-- FUNCTION: automatic updated_at
-- ---------------------------------------------------------
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ---------------------------------------------------------
-- FUNCTION: users normalization (login/email/full_name)
-- ---------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_users_normalize()
RETURNS TRIGGER AS $$
BEGIN
    NEW.login = lower(trim(NEW.login));
    NEW.email = lower(trim(NEW.email));
    NEW.full_name = trim(NEW.full_name);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ---------------------------------------------------------
-- FUNCTION: transactions validation
-- - validates direction/category_type consistency
-- - category ownership consistency is guaranteed by FK (category_id, user_id)
-- ---------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_transactions_validate()
RETURNS TRIGGER AS $$
DECLARE
    v_category_type VARCHAR(10);
BEGIN
    IF NEW.category_id IS NOT NULL THEN
        SELECT category_type
          INTO v_category_type
          FROM categories
         WHERE id = NEW.category_id;

        IF v_category_type <> NEW.direction THEN
            RAISE EXCEPTION 'Transaction direction (%) must match category type (%)',
                NEW.direction, v_category_type;
        END IF;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ---------------------------------------------------------
-- FUNCTION: budget_category_limits validation
-- - budget limit is allowed only for expense categories
-- ---------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_budget_category_limits_validate()
RETURNS TRIGGER AS $$
DECLARE
    v_category_type VARCHAR(10);
BEGIN
    SELECT category_type
      INTO v_category_type
      FROM categories
     WHERE id = NEW.category_id;

    IF v_category_type <> 'expense' THEN
        RAISE EXCEPTION 'Only expense categories can have budget limits';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ---------------------------------------------------------
-- FUNCTION: parent category validation
-- - parent and child must have the same category_type
-- ---------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_categories_validate_parent()
RETURNS TRIGGER AS $$
DECLARE
    v_parent_type VARCHAR(10);
BEGIN
    IF NEW.parent_category_id IS NOT NULL THEN
        SELECT category_type
          INTO v_parent_type
          FROM categories
         WHERE id = NEW.parent_category_id;

        IF v_parent_type <> NEW.category_type THEN
            RAISE EXCEPTION 'Parent category type (%) must match category type (%)',
                v_parent_type, NEW.category_type;
        END IF;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ---------------------------------------------------------
-- TRIGGERS: users normalize
-- ---------------------------------------------------------
DROP TRIGGER IF EXISTS trg_users_normalize ON users;
CREATE TRIGGER trg_users_normalize
    BEFORE INSERT OR UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION trg_users_normalize();

-- ---------------------------------------------------------
-- TRIGGERS: business validations
-- ---------------------------------------------------------
DROP TRIGGER IF EXISTS trg_transactions_validate ON transactions;
CREATE TRIGGER trg_transactions_validate
    BEFORE INSERT OR UPDATE ON transactions
    FOR EACH ROW
    EXECUTE FUNCTION trg_transactions_validate();

DROP TRIGGER IF EXISTS trg_budget_category_limits_validate ON budget_category_limits;
CREATE TRIGGER trg_budget_category_limits_validate
    BEFORE INSERT OR UPDATE ON budget_category_limits
    FOR EACH ROW
    EXECUTE FUNCTION trg_budget_category_limits_validate();

DROP TRIGGER IF EXISTS trg_categories_validate_parent ON categories;
CREATE TRIGGER trg_categories_validate_parent
    BEFORE INSERT OR UPDATE ON categories
    FOR EACH ROW
    EXECUTE FUNCTION trg_categories_validate_parent();

-- ---------------------------------------------------------
-- TRIGGERS: automatic updated_at
-- ---------------------------------------------------------
DROP TRIGGER IF EXISTS trg_users_set_updated_at ON users;
CREATE TRIGGER trg_users_set_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_accounts_set_updated_at ON accounts;
CREATE TRIGGER trg_accounts_set_updated_at
    BEFORE UPDATE ON accounts
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_categories_set_updated_at ON categories;
CREATE TRIGGER trg_categories_set_updated_at
    BEFORE UPDATE ON categories
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_transactions_set_updated_at ON transactions;
CREATE TRIGGER trg_transactions_set_updated_at
    BEFORE UPDATE ON transactions
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_budget_periods_set_updated_at ON budget_periods;
CREATE TRIGGER trg_budget_periods_set_updated_at
    BEFORE UPDATE ON budget_periods
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_budget_category_limits_set_updated_at ON budget_category_limits;
CREATE TRIGGER trg_budget_category_limits_set_updated_at
    BEFORE UPDATE ON budget_category_limits
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

COMMIT;
```
