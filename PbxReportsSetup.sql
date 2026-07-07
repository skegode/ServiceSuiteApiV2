-- ============================================================
-- Yeastar PBX API Report tables/procs not already present in callbox
-- Run once against the callbox database.
-- Existing (untouched) from the original PbxWebApi receiver:
--   CallAlerts / sp_insertcallalert   (action = ALERT)
--   CallRings  / sp_insertcallrings   (action = RING, BYE)
--   callcdr    / sp_insertcallcdr     (action = NewCdr)
-- ============================================================

-- ── CallAnswers (ANSWER / ANSWERED) ─────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CallAnswers')
CREATE TABLE CallAnswers (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    callid      VARCHAR(50)     NULL,
    sn          VARCHAR(50)     NULL,
    extid       INT             NULL,
    callfrom    VARCHAR(50)     NULL,
    callto      VARCHAR(50)     NULL,
    trunk       VARCHAR(50)     NULL,
    inboundid   VARCHAR(100)    NULL,
    outboundid  VARCHAR(100)    NULL,
    action      VARCHAR(50)     NULL,
    Createdate  DATETIME        NOT NULL DEFAULT GETDATE()
);
GO

IF OBJECT_ID('sp_InsertCallAnswer','P') IS NOT NULL DROP PROCEDURE sp_InsertCallAnswer;
GO
CREATE PROCEDURE sp_InsertCallAnswer
    @callid     VARCHAR(50),
    @sn         VARCHAR(50),
    @extid      INT           = NULL,
    @callfrom   VARCHAR(50)   = NULL,
    @callto     VARCHAR(50)   = NULL,
    @trunk      VARCHAR(50)   = NULL,
    @inboundid  VARCHAR(100)  = NULL,
    @outboundid VARCHAR(100)  = NULL,
    @action     VARCHAR(50)   = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO CallAnswers (callid, sn, extid, callfrom, callto, trunk, inboundid, outboundid, action)
    VALUES (@callid, @sn, @extid, @callfrom, @callto, @trunk, @inboundid, @outboundid, @action);
END
GO

-- ── CallTransfers (Tranfer) ─────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CallTransfers')
CREATE TABLE CallTransfers (
    id           INT IDENTITY(1,1) PRIMARY KEY,
    callid       VARCHAR(50)    NULL,
    sn           VARCHAR(50)    NULL,
    fromextid    INT            NULL,
    toextid      INT            NULL,
    inboundfrom  VARCHAR(50)    NULL,
    inboundto    VARCHAR(50)    NULL,
    inboundtrunk VARCHAR(50)    NULL,
    inboundid    VARCHAR(100)   NULL,
    Createdate   DATETIME       NOT NULL DEFAULT GETDATE()
);
GO

IF OBJECT_ID('sp_InsertCallTransfer','P') IS NOT NULL DROP PROCEDURE sp_InsertCallTransfer;
GO
CREATE PROCEDURE sp_InsertCallTransfer
    @callid       VARCHAR(50),
    @sn           VARCHAR(50),
    @fromextid    INT           = NULL,
    @toextid      INT           = NULL,
    @inboundfrom  VARCHAR(50)   = NULL,
    @inboundto    VARCHAR(50)   = NULL,
    @inboundtrunk VARCHAR(50)   = NULL,
    @inboundid    VARCHAR(100)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO CallTransfers (callid, sn, fromextid, toextid, inboundfrom, inboundto, inboundtrunk, inboundid)
    VALUES (@callid, @sn, @fromextid, @toextid, @inboundfrom, @inboundto, @inboundtrunk, @inboundid);
END
GO

-- ── CallFailures (CallFailed) ────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CallFailures')
CREATE TABLE CallFailures (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    callid      VARCHAR(50)    NULL,
    sn          VARCHAR(50)    NULL,
    reason      VARCHAR(100)   NULL,
    extid       INT            NULL,
    callfrom    VARCHAR(50)    NULL,
    callto      VARCHAR(50)    NULL,
    trunk       VARCHAR(50)    NULL,
    outboundid  VARCHAR(100)   NULL,
    Createdate  DATETIME       NOT NULL DEFAULT GETDATE()
);
GO

IF OBJECT_ID('sp_InsertCallFailure','P') IS NOT NULL DROP PROCEDURE sp_InsertCallFailure;
GO
CREATE PROCEDURE sp_InsertCallFailure
    @callid     VARCHAR(50),
    @sn         VARCHAR(50),
    @reason     VARCHAR(100)  = NULL,
    @extid      INT           = NULL,
    @callfrom   VARCHAR(50)   = NULL,
    @callto     VARCHAR(50)   = NULL,
    @trunk      VARCHAR(50)   = NULL,
    @outboundid VARCHAR(100)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO CallFailures (callid, sn, reason, extid, callfrom, callto, trunk, outboundid)
    VALUES (@callid, @sn, @reason, @extid, @callfrom, @callto, @trunk, @outboundid);
END
GO

-- ── CallKeypresses (DTMF) ────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CallKeypresses')
CREATE TABLE CallKeypresses (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    callid      VARCHAR(50)    NULL,
    sn          VARCHAR(50)    NULL,
    extid       INT            NULL,
    info        VARCHAR(10)    NULL,
    infos       VARCHAR(50)    NULL,
    flag        INT            NULL,
    Createdate  DATETIME       NOT NULL DEFAULT GETDATE()
);
GO

IF OBJECT_ID('sp_InsertCallKeypress','P') IS NOT NULL DROP PROCEDURE sp_InsertCallKeypress;
GO
CREATE PROCEDURE sp_InsertCallKeypress
    @callid   VARCHAR(50),
    @sn       VARCHAR(50),
    @extid    INT           = NULL,
    @info     VARCHAR(10)   = NULL,
    @infos    VARCHAR(50)   = NULL,
    @flag     INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO CallKeypresses (callid, sn, extid, info, infos, flag)
    VALUES (@callid, @sn, @extid, @info, @infos, @flag);
END
GO

-- ── ExtensionStatusLog (ExtensionStatus) ─────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ExtensionStatusLog')
CREATE TABLE ExtensionStatusLog (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    extension   VARCHAR(20)    NULL,
    status      VARCHAR(30)    NULL,
    registeredIP VARCHAR(50)   NULL,
    sn          VARCHAR(50)    NULL,
    Createdate  DATETIME       NOT NULL DEFAULT GETDATE()
);
GO

-- NOTE: PBXExtensions.Status is an existing INT column with no confirmed encoding
-- anywhere in the codebase (only ever SELECTed, never written) — Yeastar's status
-- values are text ("Registered"/"Idle"/"Busy"/...), so we do not write into it here
-- to avoid guessing a meaning for that column. History goes to ExtensionStatusLog only.
IF OBJECT_ID('sp_LogExtensionStatus','P') IS NOT NULL DROP PROCEDURE sp_LogExtensionStatus;
GO
CREATE PROCEDURE sp_LogExtensionStatus
    @extension    VARCHAR(20),
    @status       VARCHAR(30)  = NULL,
    @registeredIP VARCHAR(50)  = NULL,
    @sn           VARCHAR(50)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO ExtensionStatusLog (extension, status, registeredIP, sn)
    VALUES (@extension, @status, @registeredIP, @sn);
END
GO

-- ── SystemStartupLog (BootUp) ────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SystemStartupLog')
CREATE TABLE SystemStartupLog (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    sn          VARCHAR(50)    NULL,
    Createdate  DATETIME       NOT NULL DEFAULT GETDATE()
);
GO

IF OBJECT_ID('sp_InsertSystemStartup','P') IS NOT NULL DROP PROCEDURE sp_InsertSystemStartup;
GO
CREATE PROCEDURE sp_InsertSystemStartup
    @sn VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO SystemStartupLog (sn) VALUES (@sn);
END
GO

-- ── InboundCallEvents (Invite / Incoming) ───────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InboundCallEvents')
CREATE TABLE InboundCallEvents (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    action      VARCHAR(20)    NULL,
    callid      VARCHAR(50)    NULL,
    sn          VARCHAR(50)    NULL,
    callfrom    VARCHAR(50)    NULL,
    callto      VARCHAR(50)    NULL,
    trunk       VARCHAR(50)    NULL,
    inboundid   VARCHAR(100)   NULL,
    Createdate  DATETIME       NOT NULL DEFAULT GETDATE()
);
GO

IF OBJECT_ID('sp_InsertInboundCallEvent','P') IS NOT NULL DROP PROCEDURE sp_InsertInboundCallEvent;
GO
CREATE PROCEDURE sp_InsertInboundCallEvent
    @action     VARCHAR(20),
    @callid     VARCHAR(50),
    @sn         VARCHAR(50),
    @callfrom   VARCHAR(50)   = NULL,
    @callto     VARCHAR(50)   = NULL,
    @trunk      VARCHAR(50)   = NULL,
    @inboundid  VARCHAR(100)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO InboundCallEvents (action, callid, sn, callfrom, callto, trunk, inboundid)
    VALUES (@action, @callid, @sn, @callfrom, @callto, @trunk, @inboundid);
END
GO

SELECT 'Done. New PBX report tables/procs created.' AS Info;
GO
