CREATE
OR REPLACE FUNCTION now_ms()
    RETURNS BIGINT AS 
    $$
BEGIN
RETURN (EXTRACT(EPOCH FROM NOW()) * 1000)::BIGINT;
END;
    $$
LANGUAGE plpgsql;

CREATE
OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS 
$$
BEGIN
    NEW.updated_at
= now_ms();
RETURN NEW;
END;
$$
LANGUAGE plpgsql;

CREATE TABLE IF NOT EXISTS user_accounts (
    id UUID PRIMARY KEY,
    username TEXT NOT NULL UNIQUE,
    email TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    created_at BIGINT NOT NULL DEFAULT now_ms(),
    updated_at BIGINT NOT NULL DEFAULT now_ms()
);

CREATE TRIGGER trg_user_accounts_updated
       BEFORE UPDATE 
       ON user_accounts
    FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

CREATE TABLE user_profiles (
    user_id UUID PRIMARY KEY REFERENCES user_accounts(id) ON DELETE CASCADE,
    first_name TEXT NOT NULL,
    last_name TEXT NOT NULL,
    date_of_birth DATE,
    gender TEXT,
    created_at BIGINT NOT NULL DEFAULT now_ms(),
    updated_at BIGINT NOT NULL DEFAULT now_ms()
    );

CREATE TRIGGER trg_user_profiles_updated
    BEFORE UPDATE
    ON user_profiles
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();

CREATE TABLE chats(
    id UUID PRIMARY KEY ,
    title TEXT NOT NULL,
    owner_id UUID  NOT NULL REFERENCES user_accounts(id) ON DELETE CASCADE,
    created_at BIGINT NOT NULL DEFAULT now_ms(),
    updated_at BIGINT NOT NULL DEFAULT now_ms()
);

CREATE INDEX idx_chats_owner ON chats(owner_id);

CREATE TRIGGER trg_chats_updated
    BEFORE UPDATE
    ON chats
    FOR EACH ROW 
EXECUTE FUNCTION set_updated_at();

CREATE TABLE chat_members(
    chat_id UUID NOT NULL REFERENCES chats(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES user_accounts(id) ON DELETE CASCADE,
    joined_at BIGINT NOT NULL DEFAULT now_ms(),
    PRIMARY KEY (chat_id, user_id)
);

CREATE TABLE chat_invitations(
    id UUID PRIMARY KEY,
    chat_id UUID NOT NULL REFERENCES chats(id) ON DELETE CASCADE,
    invited_user_id UUID NOT NULL REFERENCES user_accounts(id) ON DELETE CASCADE,
    invited_by UUID NOT NULL REFERENCES user_accounts(id) ON DELETE CASCADE,
    status TEXT NOT NULL CHECK (status IN ('Pending','Approved','Rejected')),
    created_at BIGINT NOT NULL DEFAULT now_ms(),
    updated_at BIGINT NOT NULL DEFAULT now_ms()
);

CREATE INDEX idx_invites_user ON chat_invitations(invited_user_id);
CREATE INDEX idx_invites_chat ON chat_invitations(chat_id);

CREATE TRIGGER trg_chat_invitations_updated
    BEFORE UPDATE 
    ON chat_invitations
    FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

CREATE TABLE messages(
    id UUID PRIMARY KEY,
    chat_id UUID NOT NULL REFERENCES chats(id) ON DELETE CASCADE,
    sender_id UUID NULL REFERENCES user_accounts(id),
    content TEXT NOT NULL,
    created_at BIGINT NOT NULL DEFAULT now_ms()
);

CREATE INDEX idx_messages_chat ON messages(chat_id);


















