-- Activate test users so they can create series drafts (requires status_code = N'ACTIVE')
-- Run this AFTER MangaManagementSystem_Procedures_Views_Bootstrap.sql

UPDATE auth.Users
SET status_code = N'ACTIVE'
WHERE username IN (
    N'TestMangaka1',
    N'TestEditor1',
    N'TestBoardChief1',
    N'TestBoardMember1',
    N'TestAssistant1',
    N'TestAdmin'
)
  AND status_code <> N'ACTIVE';

GO

-- Verify
SELECT username, status_code
FROM auth.Users
ORDER BY username;
