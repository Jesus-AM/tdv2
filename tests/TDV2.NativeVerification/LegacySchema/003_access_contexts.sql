-- Additive local tables only; run explicitly in a prepared isolated database.
BEGIN;
CREATE TABLE tdv2_access_contexts (
    session_hash char(64) PRIMARY KEY REFERENCES tdv2_sessions(id_hash) ON DELETE CASCADE,
    revision bigint NOT NULL DEFAULT 0,
    selection text
);
COMMIT;
