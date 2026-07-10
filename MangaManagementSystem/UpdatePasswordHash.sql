USE MangaManagementDB;
GO

-- =====================================================
-- Fix BCrypt password hash for all test users.
-- Default password: "password"
-- Hash generated with BCrypt work factor 12.
-- =====================================================

UPDATE auth.Users
SET password_hash = N'$2a$12$83VooVspYNcK961JbZhbQOgk7fc0tj9R4Tmgw2Ff8XSxJedlHDlk2'
WHERE username LIKE N'Test%';

GO

-- Verify
SELECT username, status_code
FROM auth.Users
WHERE username LIKE N'Test%'
ORDER BY username;
