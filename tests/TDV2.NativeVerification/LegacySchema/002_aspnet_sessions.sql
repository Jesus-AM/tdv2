-- Additive ASP.NET structures. Application startup never runs this script.
BEGIN;
CREATE TABLE tdv2_sessions (
    id_hash char(64) PRIMARY KEY, ticket text NOT NULL, expires_at timestamptz NOT NULL
);
CREATE INDEX tdv2_sessions_expiry ON tdv2_sessions(expires_at);
CREATE TABLE tdv2_oauth_attempts (
    state_hash char(64) PRIMARY KEY, browser_hash char(64) NOT NULL, verifier text NOT NULL, expires_at timestamptz NOT NULL
);
CREATE INDEX tdv2_oauth_attempts_expiry ON tdv2_oauth_attempts(expires_at);
COMMIT;
