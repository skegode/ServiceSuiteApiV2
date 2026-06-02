
-- Table: [dbo].[RepaymentTransactions]  rows on source: 559771  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[RepaymentTransactions] ON;
INSERT INTO [dbo].[RepaymentTransactions] ([id], [Amount], [Datetransacted], [Narration], [Transtype], [loanid], [Scheduleid])
SELECT TOP 10 [id], [Amount], [Datetransacted], [Narration], [Transtype], [loanid], [Scheduleid]
FROM [dbo].[RepaymentTransactions];
SET IDENTITY_INSERT [dbo].[RepaymentTransactions] OFF;


-- Table: [dbo].[AgentActivityLog]  rows on source: 26488  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[AgentActivityLog] ON;
INSERT INTO [dbo].[AgentActivityLog] ([Id], [AgentId], [Status], [ActivityDesc], [ActivityDate], [EntityId])
SELECT TOP 10 [Id], [AgentId], [Status], [ActivityDesc], [ActivityDate], [EntityId]
FROM [dbo].[AgentActivityLog];
SET IDENTITY_INSERT [dbo].[AgentActivityLog] OFF;


-- Table: [dbo].[CallRings]  rows on source: 26137  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[CallRings] ON;
INSERT INTO [dbo].[CallRings] ([id], [callid], [extid], [sn], [callfrom], [callto], [trunk], [Createdate], [outboundid], [action])
SELECT TOP 10 [id], [callid], [extid], [sn], [callfrom], [callto], [trunk], [Createdate], [outboundid], [action]
FROM [dbo].[CallRings];
SET IDENTITY_INSERT [dbo].[CallRings] OFF;


-- Table: [dbo].[SMS]  rows on source: 20474  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[SMS] ON;
INSERT INTO [dbo].[SMS] ([ID], [Phone], [Message], [CampagnID], [ClientID], [ContractID], [RecordID], [CreatedBy], [CreatedDate], [Status], [ScheduleDate], [DateSend], [ResponseCode], [ResponseObject], [isSent], [ResponseMessageid], [cost], [ResponseStatus], [smsprovider], [companyid], [EntityId])
SELECT TOP 10 [ID], [Phone], [Message], [CampagnID], [ClientID], [ContractID], [RecordID], [CreatedBy], [CreatedDate], [Status], [ScheduleDate], [DateSend], [ResponseCode], [ResponseObject], [isSent], [ResponseMessageid], [cost], [ResponseStatus], [smsprovider], [companyid], [EntityId]
FROM [dbo].[SMS];
SET IDENTITY_INSERT [dbo].[SMS] OFF;


-- Table: [dbo].[callcdr]  rows on source: 16181  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[callcdr] ON;
INSERT INTO [dbo].[callcdr] ([id], [callid], [timestart], [callfrom], [callto], [callduraction], [talkduraction], [srctrunkname], [dsttrcunkname], [pincode], [status], [type], [recording], [didnumber], [agentringtime], [sn])
SELECT TOP 10 [id], [callid], [timestart], [callfrom], [callto], [callduraction], [talkduraction], [srctrunkname], [dsttrcunkname], [pincode], [status], [type], [recording], [didnumber], [agentringtime], [sn]
FROM [dbo].[callcdr];
SET IDENTITY_INSERT [dbo].[callcdr] OFF;


-- Table: [dbo].[CallAlerts]  rows on source: 13582  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[CallAlerts] ON;
INSERT INTO [dbo].[CallAlerts] ([id], [callid], [extid], [sn], [Createdate], [action])
SELECT TOP 10 [id], [callid], [extid], [sn], [Createdate], [action]
FROM [dbo].[CallAlerts];
SET IDENTITY_INSERT [dbo].[CallAlerts] OFF;


-- Table: [dbo].[CategoryAssignmentLog]  rows on source: 12883  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[CategoryAssignmentLog] ON;
INSERT INTO [dbo].[CategoryAssignmentLog] ([Id], [CategoryID], [AgentId], [ContractDataId], [LoanID], [AssignedAmount], [AssignedDate], [BusyScore], [EntityId])
SELECT TOP 10 [Id], [CategoryID], [AgentId], [ContractDataId], [LoanID], [AssignedAmount], [AssignedDate], [BusyScore], [EntityId]
FROM [dbo].[CategoryAssignmentLog];
SET IDENTITY_INSERT [dbo].[CategoryAssignmentLog] OFF;


-- Table: [dbo].[CallLogs]  rows on source: 12844  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[CallLogs] ON;
INSERT INTO [dbo].[CallLogs] ([ID], [CallStatus], [TimeOfCall], [RecordID], [CallDuration], [CallResponse], [PromisedAmount], [PromisedDate], [Comments], [PhoneNumber], [callid], [campaignId], [taskId], [ptpId], [Entityid], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [AgentId], [AgentName], [SmsTemplateId], [SmsBody], [NonPaymentReasonId], [CallRefType])
SELECT TOP 10 [ID], [CallStatus], [TimeOfCall], [RecordID], [CallDuration], [CallResponse], [PromisedAmount], [PromisedDate], [Comments], [PhoneNumber], [callid], [campaignId], [taskId], [ptpId], [Entityid], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [AgentId], [AgentName], [SmsTemplateId], [SmsBody], [NonPaymentReasonId], [CallRefType]
FROM [dbo].[CallLogs];
SET IDENTITY_INSERT [dbo].[CallLogs] OFF;


-- Table: [dbo].[TaskScheduler]  rows on source: 11930  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[TaskScheduler] ON;
INSERT INTO [dbo].[TaskScheduler] ([ID], [TaskAction], [TaskDate], [RecordId], [campaignId], [Comments], [TaskStatus], [IsActive], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [EntityId])
SELECT TOP 10 [ID], [TaskAction], [TaskDate], [RecordId], [campaignId], [Comments], [TaskStatus], [IsActive], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [EntityId]
FROM [dbo].[TaskScheduler];
SET IDENTITY_INSERT [dbo].[TaskScheduler] OFF;


-- Table: [dbo].[ContractData]  rows on source: 7627  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[ContractData] ON;
INSERT INTO [dbo].[ContractData] ([ID], [ContractID], [LoanID], [FirstName], [OtherNames], [PhoneNumber], [EmailAddrerss], [IDorPassport], [AmountDisbursed], [Installments], [ArrearsAmount], [DaysinArrears], [OLB], [Borrowdate], [Lonatype], [Totaldue], [Expectedduedate], [BranchId], [Branch], [OutSourcedAmount], [CategoryID], [AssignedAgent], [AssignedToAgent], [IsActioned], [ActionStatus], [updatedby], [DateUpdated], [LastActionDate], [campaignAction], [campaignId], [callbackTask], [principal], [interest], [AmountRepaid], [lastPaymentDate], [discount], [discountAmount], [loansCount], [discountID], [Penalty], [isDiscounted], [Entityid], [IsDeleted], [LoanRefId], [BorrowerRefId], [lastcomment], [ptpamount], [ptpid], [InitialOutSourcedAmount], [IsClosed])
SELECT TOP 10 [ID], [ContractID], [LoanID], [FirstName], [OtherNames], [PhoneNumber], [EmailAddrerss], [IDorPassport], [AmountDisbursed], [Installments], [ArrearsAmount], [DaysinArrears], [OLB], [Borrowdate], [Lonatype], [Totaldue], [Expectedduedate], [BranchId], [Branch], [OutSourcedAmount], [CategoryID], [AssignedAgent], [AssignedToAgent], [IsActioned], [ActionStatus], [updatedby], [DateUpdated], [LastActionDate], [campaignAction], [campaignId], [callbackTask], [principal], [interest], [AmountRepaid], [lastPaymentDate], [discount], [discountAmount], [loansCount], [discountID], [Penalty], [isDiscounted], [Entityid], [IsDeleted], [LoanRefId], [BorrowerRefId], [lastcomment], [ptpamount], [ptpid], [InitialOutSourcedAmount], [IsClosed]
FROM [dbo].[ContractData];
SET IDENTITY_INSERT [dbo].[ContractData] OFF;


-- Table: [dbo].[RepaymentHistory]  rows on source: 708  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[RepaymentHistory] ON;
INSERT INTO [dbo].[RepaymentHistory] ([id], [AgentId], [ContractId], [ContractDataId], [InitialOlb], [AmountPaid], [NewOlb], [Entityid], [Repaymentid], [PaymentDate], [IsReversed], [ReversedBy], [ReversedDate], [ReversalReason], [UpdatedBy], [UpdatedDate])
SELECT TOP 10 [id], [AgentId], [ContractId], [ContractDataId], [InitialOlb], [AmountPaid], [NewOlb], [Entityid], [Repaymentid], [PaymentDate], [IsReversed], [ReversedBy], [ReversedDate], [ReversalReason], [UpdatedBy], [UpdatedDate]
FROM [dbo].[RepaymentHistory];
SET IDENTITY_INSERT [dbo].[RepaymentHistory] OFF;


-- Table: [dbo].[PromisedToPay]  rows on source: 548  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[PromisedToPay] ON;
INSERT INTO [dbo].[PromisedToPay] ([ID], [PromisedAmount], [PromisedDate], [PaymentStatus], [RecordID], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [AmountPaid], [Dateofpayment], [campaignId], [closed], [EntityId])
SELECT TOP 10 [ID], [PromisedAmount], [PromisedDate], [PaymentStatus], [RecordID], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [AmountPaid], [Dateofpayment], [campaignId], [closed], [EntityId]
FROM [dbo].[PromisedToPay];
SET IDENTITY_INSERT [dbo].[PromisedToPay] OFF;


-- Table: [dbo].[ContractLmsLogs]  rows on source: 231  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[ContractLmsLogs] ON;
INSERT INTO [dbo].[ContractLmsLogs] ([Id], [ContractId], [Records], [CreatedDate])
SELECT TOP 10 [Id], [ContractId], [Records], [CreatedDate]
FROM [dbo].[ContractLmsLogs];
SET IDENTITY_INSERT [dbo].[ContractLmsLogs] OFF;


-- Table: [dbo].[Contractallocation]  rows on source: 216  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[Contractallocation] ON;
INSERT INTO [dbo].[Contractallocation] ([Id], [AgentId], [ContractCount], [ContractSummation], [CategoryId], [InitiatedDate])
SELECT TOP 10 [Id], [AgentId], [ContractCount], [ContractSummation], [CategoryId], [InitiatedDate]
FROM [dbo].[Contractallocation];
SET IDENTITY_INSERT [dbo].[Contractallocation] OFF;


-- Table: [dbo].[AgentBreakLog]  rows on source: 110  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[AgentBreakLog] ON;
INSERT INTO [dbo].[AgentBreakLog] ([Id], [AgentId], [BreakTypeId], [BreakStart], [BreakEnd], [DurationMinutes], [Notes], [EntityId], [CreatedBy], [DateCreated])
SELECT TOP 10 [Id], [AgentId], [BreakTypeId], [BreakStart], [BreakEnd], [DurationMinutes], [Notes], [EntityId], [CreatedBy], [DateCreated]
FROM [dbo].[AgentBreakLog];
SET IDENTITY_INSERT [dbo].[AgentBreakLog] OFF;


-- Table: [dbo].[sms_templates]  rows on source: 39  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[sms_templates] ON;
INSERT INTO [dbo].[sms_templates] ([id], [name], [body], [category], [is_active], [created_by], [created_at], [updated_at])
SELECT TOP 10 [id], [name], [body], [category], [is_active], [created_by], [created_at], [updated_at]
FROM [dbo].[sms_templates];
SET IDENTITY_INSERT [dbo].[sms_templates] OFF;


-- Table: [dbo].[TicketChecklistProgress]  rows on source: 36  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[TicketChecklistProgress] ON;
INSERT INTO [dbo].[TicketChecklistProgress] ([ID], [TicketId], [ChecklistItemId], [IsCompleted], [CompletedBy], [CompletedDate], [Notes])
SELECT TOP 10 [ID], [TicketId], [ChecklistItemId], [IsCompleted], [CompletedBy], [CompletedDate], [Notes]
FROM [dbo].[TicketChecklistProgress];
SET IDENTITY_INSERT [dbo].[TicketChecklistProgress] OFF;


-- Table: [dbo].[QA_ScorecardLine]  rows on source: 30  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[QA_ScorecardLine] ON;
INSERT INTO [dbo].[QA_ScorecardLine] ([LineId], [ScorecardId], [CriteriaId], [Score], [WeightedScore], [Notes], [CreatedAt], [UpdatedAt])
SELECT TOP 10 [LineId], [ScorecardId], [CriteriaId], [Score], [WeightedScore], [Notes], [CreatedAt], [UpdatedAt]
FROM [dbo].[QA_ScorecardLine];
SET IDENTITY_INSERT [dbo].[QA_ScorecardLine] OFF;


-- Table: [dbo].[CategoryAgents]  rows on source: 27  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[CategoryAgents] ON;
INSERT INTO [dbo].[CategoryAgents] ([ID], [CategoryID], [AgentId], [AgentName], [Branches])
SELECT TOP 10 [ID], [CategoryID], [AgentId], [AgentName], [Branches]
FROM [dbo].[CategoryAgents];
SET IDENTITY_INSERT [dbo].[CategoryAgents] OFF;


-- Table: [dbo].[ContractLmsConnections]  rows on source: 18  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[ContractLmsConnections] ON;
INSERT INTO [dbo].[ContractLmsConnections] ([Id], [ContractId], [MinDays], [MaxDays], [MinAmount], [MaxAmount], [MinOlb], [MaxOlb], [MinArrears], [MaxArrears], [DataRefreshCycle], [DataSyncMode], [PaymentsRefreshCycle], [PaymentsSyncMode], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [LastDataFetch], [NextDataFetch], [LastPaymentsFetch], [NextPaymentsFetch])
SELECT TOP 10 [Id], [ContractId], [MinDays], [MaxDays], [MinAmount], [MaxAmount], [MinOlb], [MaxOlb], [MinArrears], [MaxArrears], [DataRefreshCycle], [DataSyncMode], [PaymentsRefreshCycle], [PaymentsSyncMode], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [LastDataFetch], [NextDataFetch], [LastPaymentsFetch], [NextPaymentsFetch]
FROM [dbo].[ContractLmsConnections];
SET IDENTITY_INSERT [dbo].[ContractLmsConnections] OFF;


-- Table: [dbo].[TicketResponses]  rows on source: 17  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[TicketResponses] ON;
INSERT INTO [dbo].[TicketResponses] ([ID], [TicketId], [ResponseType], [Message], [IsInternal], [CreatedBy], [CreatedByName], [CreatedDate])
SELECT TOP 10 [ID], [TicketId], [ResponseType], [Message], [IsInternal], [CreatedBy], [CreatedByName], [CreatedDate]
FROM [dbo].[TicketResponses];
SET IDENTITY_INSERT [dbo].[TicketResponses] OFF;


-- Table: [dbo].[CallResponse]  rows on source: 15  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[CallResponse] ON;
INSERT INTO [dbo].[CallResponse] ([ID], [contractType], [Response], [CallStatus], [ScheduleTask], [ScheduleAction], [IsActive], [EntityId], [CreatedBy], [DateCreated])
SELECT TOP 10 [ID], [contractType], [Response], [CallStatus], [ScheduleTask], [ScheduleAction], [IsActive], [EntityId], [CreatedBy], [DateCreated]
FROM [dbo].[CallResponse];
SET IDENTITY_INSERT [dbo].[CallResponse] OFF;


-- Table: [dbo].[QA_Criteria]  rows on source: 15  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[QA_Criteria] ON;
INSERT INTO [dbo].[QA_Criteria] ([CriteriaId], [CategoryId], [CriteriaText], [CriteriaWeight], [IsActive], [SortOrder], [CreatedAt], [UpdatedAt])
SELECT TOP 10 [CriteriaId], [CategoryId], [CriteriaText], [CriteriaWeight], [IsActive], [SortOrder], [CreatedAt], [UpdatedAt]
FROM [dbo].[QA_Criteria];
SET IDENTITY_INSERT [dbo].[QA_Criteria] OFF;


-- Table: [dbo].[ReasonForNonPayment]  rows on source: 11  (copying TOP 10)
SET IDENTITY_INSERT [dbo].[ReasonForNonPayment] ON;
INSERT INTO [dbo].[ReasonForNonPayment] ([ID], [Reason], [Description], [ContractType], [IsActive], [SortOrder], [EntityId], [CreatedBy], [DateCreated])
SELECT TOP 10 [ID], [Reason], [Description], [ContractType], [IsActive], [SortOrder], [EntityId], [CreatedBy], [DateCreated]
FROM [dbo].[ReasonForNonPayment];
SET IDENTITY_INSERT [dbo].[ReasonForNonPayment] OFF;


-- Table: [dbo].[SmsTemplate]  rows on source: 10  (copying all)
SET IDENTITY_INSERT [dbo].[SmsTemplate] ON;
INSERT INTO [dbo].[SmsTemplate] ([ID], [Title], [Template], [ClientID], [ContractID], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [EntityId])
SELECT [ID], [Title], [Template], [ClientID], [ContractID], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [EntityId]
FROM [dbo].[SmsTemplate];
SET IDENTITY_INSERT [dbo].[SmsTemplate] OFF;


-- Table: [dbo].[Categories]  rows on source: 9  (copying all)
SET IDENTITY_INSERT [dbo].[Categories] ON;
INSERT INTO [dbo].[Categories] ([ID], [ContractyID], [CategoryName], [Description], [MinDays], [MaxDays], [MinAmount], [MaxAmount], [MinOlb], [MaxOlb], [MinArrears], [MaxArrears], [Priority], [Color], [IsActive], [EntityId], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate])
SELECT [ID], [ContractyID], [CategoryName], [Description], [MinDays], [MaxDays], [MinAmount], [MaxAmount], [MinOlb], [MaxOlb], [MinArrears], [MaxArrears], [Priority], [Color], [IsActive], [EntityId], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate]
FROM [dbo].[Categories];
SET IDENTITY_INSERT [dbo].[Categories] OFF;


-- Table: [dbo].[TicketAssignments]  rows on source: 9  (copying all)
SET IDENTITY_INSERT [dbo].[TicketAssignments] ON;
INSERT INTO [dbo].[TicketAssignments] ([ID], [TicketId], [AgentId], [AgentName], [AssignedBy], [AssignedDate], [RelievedDate], [IsActive], [Notes])
SELECT [ID], [TicketId], [AgentId], [AgentName], [AssignedBy], [AssignedDate], [RelievedDate], [IsActive], [Notes]
FROM [dbo].[TicketAssignments];
SET IDENTITY_INSERT [dbo].[TicketAssignments] OFF;


-- Table: [dbo].[CallCampaigns]  rows on source: 8  (copying all)
SET IDENTITY_INSERT [dbo].[CallCampaigns] ON;
INSERT INTO [dbo].[CallCampaigns] ([ID], [Title], [StartDate], [DueDate], [CampaignType], [Records], [Agents], [ActionedRecords], [ContractID], [CategoryID], [minAmount], [maxAmount], [rangeFrom], [rangeTo], [Status], [CreatedBy], [CreatedDate], [campainNote], [UpdatedBy], [UpdatedDate], [Entityid])
SELECT [ID], [Title], [StartDate], [DueDate], [CampaignType], [Records], [Agents], [ActionedRecords], [ContractID], [CategoryID], [minAmount], [maxAmount], [rangeFrom], [rangeTo], [Status], [CreatedBy], [CreatedDate], [campainNote], [UpdatedBy], [UpdatedDate], [Entityid]
FROM [dbo].[CallCampaigns];
SET IDENTITY_INSERT [dbo].[CallCampaigns] OFF;


-- Table: [dbo].[CallScheduleTasks]  rows on source: 8  (copying all)
INSERT INTO [dbo].[CallScheduleTasks] ([id], [label], [icon])
SELECT [id], [label], [icon]
FROM [dbo].[CallScheduleTasks];


-- Table: [dbo].[Agents]  rows on source: 7  (copying all)
SET IDENTITY_INSERT [dbo].[Agents] ON;
INSERT INTO [dbo].[Agents] ([Id], [AgentId], [AgentName], [Extension], [ExtensionPswd], [Status], [LastUpdated], [SupervisorId], [SupervisorName], [EntityId])
SELECT [Id], [AgentId], [AgentName], [Extension], [ExtensionPswd], [Status], [LastUpdated], [SupervisorId], [SupervisorName], [EntityId]
FROM [dbo].[Agents];
SET IDENTITY_INSERT [dbo].[Agents] OFF;


-- Table: [dbo].[Contracts]  rows on source: 7  (copying all)
SET IDENTITY_INSERT [dbo].[Contracts] ON;
INSERT INTO [dbo].[Contracts] ([ID], [ContractName], [CompanyID], [FileAttached], [Filepath], [isActive], [ExpiryDate], [DataSource], [ContractType], [CategoryId], [Entityid], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate])
SELECT [ID], [ContractName], [CompanyID], [FileAttached], [Filepath], [isActive], [ExpiryDate], [DataSource], [ContractType], [CategoryId], [Entityid], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate]
FROM [dbo].[Contracts];
SET IDENTITY_INSERT [dbo].[Contracts] OFF;


-- Table: [dbo].[RefreshCycles]  rows on source: 7  (copying all)
INSERT INTO [dbo].[RefreshCycles] ([Id], [Name], [DurationMinutes])
SELECT [Id], [Name], [DurationMinutes]
FROM [dbo].[RefreshCycles];


-- Table: [dbo].[AgentBreakType]  rows on source: 6  (copying all)
SET IDENTITY_INSERT [dbo].[AgentBreakType] ON;
INSERT INTO [dbo].[AgentBreakType] ([Id], [BreakName], [MaxDurationMinutes], [IsPaid], [IsActive], [EntityId], [CreatedBy], [DateCreated])
SELECT [Id], [BreakName], [MaxDurationMinutes], [IsPaid], [IsActive], [EntityId], [CreatedBy], [DateCreated]
FROM [dbo].[AgentBreakType];
SET IDENTITY_INSERT [dbo].[AgentBreakType] OFF;


-- Table: [dbo].[QA_ScorecardCategoryScore]  rows on source: 6  (copying all)
SET IDENTITY_INSERT [dbo].[QA_ScorecardCategoryScore] ON;
INSERT INTO [dbo].[QA_ScorecardCategoryScore] ([CategoryScoreId], [ScorecardId], [CategoryId], [EarnedScore], [MaxScore], [IsPass], [CreatedAt], [UpdatedAt])
SELECT [CategoryScoreId], [ScorecardId], [CategoryId], [EarnedScore], [MaxScore], [IsPass], [CreatedAt], [UpdatedAt]
FROM [dbo].[QA_ScorecardCategoryScore];
SET IDENTITY_INSERT [dbo].[QA_ScorecardCategoryScore] OFF;


-- Table: [dbo].[Tickets]  rows on source: 6  (copying all)
SET IDENTITY_INSERT [dbo].[Tickets] ON;
INSERT INTO [dbo].[Tickets] ([ID], [EntityId], [TicketTypeId], [TicketRef], [Title], [Description], [FirstName], [OtherNames], [PhoneNumber], [EmailAddrerss], [IDorPassport], [LoanID], [LoanRefId], [BorrowerRefId], [Status], [Priority], [AssignedAgent], [AssignedAgentName], [AssignedDate], [DueDate], [ResolvedDate], [ClosedDate], [Origin], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate])
SELECT [ID], [EntityId], [TicketTypeId], [TicketRef], [Title], [Description], [FirstName], [OtherNames], [PhoneNumber], [EmailAddrerss], [IDorPassport], [LoanID], [LoanRefId], [BorrowerRefId], [Status], [Priority], [AssignedAgent], [AssignedAgentName], [AssignedDate], [DueDate], [ResolvedDate], [ClosedDate], [Origin], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate]
FROM [dbo].[Tickets];
SET IDENTITY_INSERT [dbo].[Tickets] OFF;


-- Table: [dbo].[TicketTypeChecklists]  rows on source: 6  (copying all)
SET IDENTITY_INSERT [dbo].[TicketTypeChecklists] ON;
INSERT INTO [dbo].[TicketTypeChecklists] ([ID], [TicketTypeId], [ItemText], [IsRequired], [SortOrder], [CreatedBy], [CreatedDate])
SELECT [ID], [TicketTypeId], [ItemText], [IsRequired], [SortOrder], [CreatedBy], [CreatedDate]
FROM [dbo].[TicketTypeChecklists];
SET IDENTITY_INSERT [dbo].[TicketTypeChecklists] OFF;


-- Table: [dbo].[CallScheduleActions]  rows on source: 5  (copying all)
INSERT INTO [dbo].[CallScheduleActions] ([id], [label])
SELECT [id], [label]
FROM [dbo].[CallScheduleActions];


-- Table: [dbo].[CallStatuses]  rows on source: 5  (copying all)
INSERT INTO [dbo].[CallStatuses] ([id], [label], [icon])
SELECT [id], [label], [icon]
FROM [dbo].[CallStatuses];


-- Table: [dbo].[TicketAnswers]  rows on source: 5  (copying all)
SET IDENTITY_INSERT [dbo].[TicketAnswers] ON;
INSERT INTO [dbo].[TicketAnswers] ([ID], [TicketId], [QuestionId], [AnswerText], [AnsweredBy], [AnsweredDate])
SELECT [ID], [TicketId], [QuestionId], [AnswerText], [AnsweredBy], [AnsweredDate]
FROM [dbo].[TicketAnswers];
SET IDENTITY_INSERT [dbo].[TicketAnswers] OFF;


-- Table: [dbo].[TicketTypeQuestions]  rows on source: 5  (copying all)
SET IDENTITY_INSERT [dbo].[TicketTypeQuestions] ON;
INSERT INTO [dbo].[TicketTypeQuestions] ([ID], [TicketTypeId], [QuestionText], [AnswerType], [Options], [IsRequired], [SortOrder], [CreatedBy], [CreatedDate])
SELECT [ID], [TicketTypeId], [QuestionText], [AnswerType], [Options], [IsRequired], [SortOrder], [CreatedBy], [CreatedDate]
FROM [dbo].[TicketTypeQuestions];
SET IDENTITY_INSERT [dbo].[TicketTypeQuestions] OFF;


-- Table: [dbo].[RankingWeights]  rows on source: 4  (copying all)
SET IDENTITY_INSERT [dbo].[RankingWeights] ON;
INSERT INTO [dbo].[RankingWeights] ([Id], [WeightPoint], [Initial], [RankName])
SELECT [Id], [WeightPoint], [Initial], [RankName]
FROM [dbo].[RankingWeights];
SET IDENTITY_INSERT [dbo].[RankingWeights] OFF;


-- Table: [dbo].[Branches]  rows on source: 3  (copying all)
SET IDENTITY_INSERT [dbo].[Branches] ON;
INSERT INTO [dbo].[Branches] ([ID], [CompanyID], [BranchReference], [BranchName], [Address], [Region], [IsActive], [EntityIdy], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [Entityid])
SELECT [ID], [CompanyID], [BranchReference], [BranchName], [Address], [Region], [IsActive], [EntityIdy], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [Entityid]
FROM [dbo].[Branches];
SET IDENTITY_INSERT [dbo].[Branches] OFF;


-- Table: [dbo].[company_products]  rows on source: 3  (copying all)
SET IDENTITY_INSERT [dbo].[company_products] ON;
INSERT INTO [dbo].[company_products] ([product_id], [product_name], [company_id], [EntityId])
SELECT [product_id], [product_name], [company_id], [EntityId]
FROM [dbo].[company_products];
SET IDENTITY_INSERT [dbo].[company_products] OFF;


-- Table: [dbo].[ContractTypes]  rows on source: 3  (copying all)
INSERT INTO [dbo].[ContractTypes] ([Id], [Reference], [Title], [Description])
SELECT [Id], [Reference], [Title], [Description]
FROM [dbo].[ContractTypes];


-- Table: [dbo].[QA_Category]  rows on source: 3  (copying all)
SET IDENTITY_INSERT [dbo].[QA_Category] ON;
INSERT INTO [dbo].[QA_Category] ([CategoryId], [CategoryNumber], [CategoryName], [TotalWeight], [PassScore], [IsActive], [CreatedAt], [UpdatedAt], [EntityId])
SELECT [CategoryId], [CategoryNumber], [CategoryName], [TotalWeight], [PassScore], [IsActive], [CreatedAt], [UpdatedAt], [EntityId]
FROM [dbo].[QA_Category];
SET IDENTITY_INSERT [dbo].[QA_Category] OFF;


-- Table: [dbo].[TaskAction]  rows on source: 3  (copying all)
SET IDENTITY_INSERT [dbo].[TaskAction] ON;
INSERT INTO [dbo].[TaskAction] ([ID], [ActionName])
SELECT [ID], [ActionName]
FROM [dbo].[TaskAction];
SET IDENTITY_INSERT [dbo].[TaskAction] OFF;


-- Table: [dbo].[TicketTypeAgents]  rows on source: 3  (copying all)
SET IDENTITY_INSERT [dbo].[TicketTypeAgents] ON;
INSERT INTO [dbo].[TicketTypeAgents] ([ID], [TicketTypeId], [AgentId], [AgentName], [IsActive], [AddedBy], [AddedDate])
SELECT [ID], [TicketTypeId], [AgentId], [AgentName], [IsActive], [AddedBy], [AddedDate]
FROM [dbo].[TicketTypeAgents];
SET IDENTITY_INSERT [dbo].[TicketTypeAgents] OFF;


-- Table: [dbo].[CallStatus]  rows on source: 2  (copying all)
SET IDENTITY_INSERT [dbo].[CallStatus] ON;
INSERT INTO [dbo].[CallStatus] ([ID], [CallStatus])
SELECT [ID], [CallStatus]
FROM [dbo].[CallStatus];
SET IDENTITY_INSERT [dbo].[CallStatus] OFF;


-- Table: [dbo].[Companies]  rows on source: 2  (copying all)
SET IDENTITY_INSERT [dbo].[Companies] ON;
INSERT INTO [dbo].[Companies] ([ID], [CompanyName], [CompanyPhoneNo], [CompanyEmail], [ContactPerson], [ContactPersonEmail], [ContactPersonPhoneNo], [CompanyKRApin], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [CompanyPaybill], [AccountType], [kopapaybill], [Entityid], [LmsEntityId])
SELECT [ID], [CompanyName], [CompanyPhoneNo], [CompanyEmail], [ContactPerson], [ContactPersonEmail], [ContactPersonPhoneNo], [CompanyKRApin], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [CompanyPaybill], [AccountType], [kopapaybill], [Entityid], [LmsEntityId]
FROM [dbo].[Companies];
SET IDENTITY_INSERT [dbo].[Companies] OFF;


-- Table: [dbo].[ContractDataSources]  rows on source: 2  (copying all)
INSERT INTO [dbo].[ContractDataSources] ([Id], [Reference], [Title], [Description])
SELECT [Id], [Reference], [Title], [Description]
FROM [dbo].[ContractDataSources];


-- Table: [dbo].[QA_Scorecard]  rows on source: 2  (copying all)
SET IDENTITY_INSERT [dbo].[QA_Scorecard] ON;
INSERT INTO [dbo].[QA_Scorecard] ([ScorecardId], [AgentId], [AgentName], [CallId], [CallDate], [ContractDataId], [ReferenceNumber], [EvaluatorId], [EvaluatorName], [Comments], [TotalScore], [IsPass], [Status], [EntityId], [CreatedAt], [UpdatedAt])
SELECT [ScorecardId], [AgentId], [AgentName], [CallId], [CallDate], [ContractDataId], [ReferenceNumber], [EvaluatorId], [EvaluatorName], [Comments], [TotalScore], [IsPass], [Status], [EntityId], [CreatedAt], [UpdatedAt]
FROM [dbo].[QA_Scorecard];
SET IDENTITY_INSERT [dbo].[QA_Scorecard] OFF;


-- Table: [dbo].[SmsCampaigns]  rows on source: 2  (copying all)
SET IDENTITY_INSERT [dbo].[SmsCampaigns] ON;
INSERT INTO [dbo].[SmsCampaigns] ([ID], [Title], [TemplateID], [Template], [ClientID], [ContractID], [SMSs], [Send], [Failed], [Status], [ScheduleDate], [CreatedBy], [CreatedDate], [EntityId], [unitId], [AgentId], [MinDaysInArrears], [MaxDaysInArrears], [MinDueDay], [MaxDueDay])
SELECT [ID], [Title], [TemplateID], [Template], [ClientID], [ContractID], [SMSs], [Send], [Failed], [Status], [ScheduleDate], [CreatedBy], [CreatedDate], [EntityId], [unitId], [AgentId], [MinDaysInArrears], [MaxDaysInArrears], [MinDueDay], [MaxDueDay]
FROM [dbo].[SmsCampaigns];
SET IDENTITY_INSERT [dbo].[SmsCampaigns] OFF;


-- Table: [dbo].[SmsPlaceholders]  rows on source: 2  (copying all)
SET IDENTITY_INSERT [dbo].[SmsPlaceholders] ON;
INSERT INTO [dbo].[SmsPlaceholders] ([ID], [Title], [Code], [Status], [CreatedBy], [CreatedDate])
SELECT [ID], [Title], [Code], [Status], [CreatedBy], [CreatedDate]
FROM [dbo].[SmsPlaceholders];
SET IDENTITY_INSERT [dbo].[SmsPlaceholders] OFF;


-- Table: [dbo].[LmsIntegration]  rows on source: 1  (copying all)
SET IDENTITY_INSERT [dbo].[LmsIntegration] ON;
INSERT INTO [dbo].[LmsIntegration] ([ID], [lmsApiUrl], [lmsApiKey], [lmsActive], [EntityId], [CreatedBy], [DateCreated])
SELECT [ID], [lmsApiUrl], [lmsApiKey], [lmsActive], [EntityId], [CreatedBy], [DateCreated]
FROM [dbo].[LmsIntegration];
SET IDENTITY_INSERT [dbo].[LmsIntegration] OFF;


-- Table: [dbo].[TicketTypeAssignCursor]  rows on source: 1  (copying all)
INSERT INTO [dbo].[TicketTypeAssignCursor] ([TicketTypeId], [LastAgentIndex], [UpdatedDate])
SELECT [TicketTypeId], [LastAgentIndex], [UpdatedDate]
FROM [dbo].[TicketTypeAssignCursor];


-- Table: [dbo].[TicketTypes]  rows on source: 1  (copying all)
SET IDENTITY_INSERT [dbo].[TicketTypes] ON;
INSERT INTO [dbo].[TicketTypes] ([ID], [EntityId], [Title], [Description], [Color], [Priority], [PriorityTitle], [SLAMinutes], [AutoAssign], [IsActive], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate])
SELECT [ID], [EntityId], [Title], [Description], [Color], [Priority], [PriorityTitle], [SLAMinutes], [AutoAssign], [IsActive], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate]
FROM [dbo].[TicketTypes];
SET IDENTITY_INSERT [dbo].[TicketTypes] OFF;


-- Table: [dbo].[AgentStatus]  rows on source: 0  (copying all)
SET IDENTITY_INSERT [dbo].[AgentStatus] ON;
INSERT INTO [dbo].[AgentStatus] ([Id], [AgentId], [AgentName], [Status], [LastUpdated], [EntityId])
SELECT [Id], [AgentId], [AgentName], [Status], [LastUpdated], [EntityId]
FROM [dbo].[AgentStatus];
SET IDENTITY_INSERT [dbo].[AgentStatus] OFF;


-- Table: [dbo].[AgentTransferLog]  rows on source: 0  (copying all)
SET IDENTITY_INSERT [dbo].[AgentTransferLog] ON;
INSERT INTO [dbo].[AgentTransferLog] ([ID], [ContractId], [CategoryId], [ContractDataId], [LoanID], [FromAgentId], [ToAgentId], [TransferredOLB], [TransferredBy], [Reason], [EntityId], [TransferDate])
SELECT [ID], [ContractId], [CategoryId], [ContractDataId], [LoanID], [FromAgentId], [ToAgentId], [TransferredOLB], [TransferredBy], [Reason], [EntityId], [TransferDate]
FROM [dbo].[AgentTransferLog];
SET IDENTITY_INSERT [dbo].[AgentTransferLog] OFF;


-- Table: [dbo].[ContractDiscounts]  rows on source: 0  (copying all)
SET IDENTITY_INSERT [dbo].[ContractDiscounts] ON;
INSERT INTO [dbo].[ContractDiscounts] ([ID], [Title], [ContractID], [CompanyID], [minDueDate], [maxDueDate], [minDisbursementDate], [maxDisbursementDate], [minBalance], [maxBalance], [DiscountPercentage], [StartDate], [EndDate], [Records], [Agents], [Status], [CreatedBy], [CreatedDate])
SELECT [ID], [Title], [ContractID], [CompanyID], [minDueDate], [maxDueDate], [minDisbursementDate], [maxDisbursementDate], [minBalance], [maxBalance], [DiscountPercentage], [StartDate], [EndDate], [Records], [Agents], [Status], [CreatedBy], [CreatedDate]
FROM [dbo].[ContractDiscounts];
SET IDENTITY_INSERT [dbo].[ContractDiscounts] OFF;


-- Table: [dbo].[DiscountApplications]  rows on source: 0  (copying all)
SET IDENTITY_INSERT [dbo].[DiscountApplications] ON;
INSERT INTO [dbo].[DiscountApplications] ([ID], [DiscountID], [ContractDataID], [OriginalAmount], [DiscountAmount], [DiscountPct], [SettledAmount], [Status], [AcceptedDate], [ExpiryDate], [Entityid], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate])
SELECT [ID], [DiscountID], [ContractDataID], [OriginalAmount], [DiscountAmount], [DiscountPct], [SettledAmount], [Status], [AcceptedDate], [ExpiryDate], [Entityid], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate]
FROM [dbo].[DiscountApplications];
SET IDENTITY_INSERT [dbo].[DiscountApplications] OFF;


-- Table: [dbo].[Discounts]  rows on source: 0  (copying all)
SET IDENTITY_INSERT [dbo].[Discounts] ON;
INSERT INTO [dbo].[Discounts] ([ID], [Title], [Description], [DiscountType], [DiscountValue], [MaxDiscountCap], [ContractID], [CategoryID], [ValidFrom], [ValidTo], [IsActive], [ApprovalStatus], [ApprovedBy], [ApprovedDate], [Entityid], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate])
SELECT [ID], [Title], [Description], [DiscountType], [DiscountValue], [MaxDiscountCap], [ContractID], [CategoryID], [ValidFrom], [ValidTo], [IsActive], [ApprovalStatus], [ApprovedBy], [ApprovedDate], [Entityid], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate]
FROM [dbo].[Discounts];
SET IDENTITY_INSERT [dbo].[Discounts] OFF;


-- Table: [dbo].[RepaymentData]  rows on source: 0  (copying all)
SET IDENTITY_INSERT [dbo].[RepaymentData] ON;
INSERT INTO [dbo].[RepaymentData] ([ID], [LoanID], [FirstName], [OtherNames], [PhoneNumber], [IDorPassport], [AmountDisbursed], [Installments], [ArrearsAmount], [DaysinArrears], [OLB], [Branch], [RepaymentID], [IsReconciled], [Narration], [Contractid])
SELECT [ID], [LoanID], [FirstName], [OtherNames], [PhoneNumber], [IDorPassport], [AmountDisbursed], [Installments], [ArrearsAmount], [DaysinArrears], [OLB], [Branch], [RepaymentID], [IsReconciled], [Narration], [Contractid]
FROM [dbo].[RepaymentData];
SET IDENTITY_INSERT [dbo].[RepaymentData] OFF;


-- Table: [dbo].[Repayments]  rows on source: 0  (copying all)
SET IDENTITY_INSERT [dbo].[Repayments] ON;
INSERT INTO [dbo].[Repayments] ([ID], [ContractID], [RepaymentTitle], [FileAttached], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [Filepath], [IsDisabled], [Entityid])
SELECT [ID], [ContractID], [RepaymentTitle], [FileAttached], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [Filepath], [IsDisabled], [Entityid]
FROM [dbo].[Repayments];
SET IDENTITY_INSERT [dbo].[Repayments] OFF;


-- Table: [dbo].[sms_campaigns]  rows on source: 0  (copying all)
SET IDENTITY_INSERT [dbo].[sms_campaigns] ON;
INSERT INTO [dbo].[sms_campaigns] ([id], [name], [description], [message], [sender_id], [template_id], [recipient_type], [source_campaign_id], [contract_id], [manual_numbers], [status], [total_count], [sent_count], [delivered_count], [failed_count], [send_immediately], [scheduled_at], [started_at], [completed_at], [created_by], [created_at], [updated_at])
SELECT [id], [name], [description], [message], [sender_id], [template_id], [recipient_type], [source_campaign_id], [contract_id], [manual_numbers], [status], [total_count], [sent_count], [delivered_count], [failed_count], [send_immediately], [scheduled_at], [started_at], [completed_at], [created_by], [created_at], [updated_at]
FROM [dbo].[sms_campaigns];
SET IDENTITY_INSERT [dbo].[sms_campaigns] OFF;


-- Table: [dbo].[sms_logs]  rows on source: 0  (copying all)
SET IDENTITY_INSERT [dbo].[sms_logs] ON;
INSERT INTO [dbo].[sms_logs] ([id], [phone], [message], [sender_id], [template_id], [campaign_id], [record_id], [scheduled_id], [status], [scheduled_at], [sent_at], [delivered_at], [error_message], [external_id], [message_id], [cost], [created_at])
SELECT [id], [phone], [message], [sender_id], [template_id], [campaign_id], [record_id], [scheduled_id], [status], [scheduled_at], [sent_at], [delivered_at], [error_message], [external_id], [message_id], [cost], [created_at]
FROM [dbo].[sms_logs];
SET IDENTITY_INSERT [dbo].[sms_logs] OFF;


-- Table: [dbo].[sms_scheduled]  rows on source: 0  (copying all)
SET IDENTITY_INSERT [dbo].[sms_scheduled] ON;
INSERT INTO [dbo].[sms_scheduled] ([id], [phone], [message], [sender_id], [template_id], [record_id], [campaign_id], [status], [scheduled_at], [sent_at], [delivered_at], [error_message], [external_id], [cost], [created_by], [created_at], [updated_at])
SELECT [id], [phone], [message], [sender_id], [template_id], [record_id], [campaign_id], [status], [scheduled_at], [sent_at], [delivered_at], [error_message], [external_id], [cost], [created_by], [created_at], [updated_at]
FROM [dbo].[sms_scheduled];
SET IDENTITY_INSERT [dbo].[sms_scheduled] OFF;


-- Table: [dbo].[TicketTypeLmsConfig]  rows on source: 0  (copying all)
SET IDENTITY_INSERT [dbo].[TicketTypeLmsConfig] ON;
INSERT INTO [dbo].[TicketTypeLmsConfig] ([ID], [TicketTypeId], [MinDuration], [MaxDuration], [MinAmount], [MaxAmount], [MinOlb], [MaxOlb], [MinArrearsDays], [MaxArrearsDays], [MinArrears], [MaxArrears], [DataRefreshCycle], [DataSyncMode], [LastSyncDate], [NextSyncDate], [IsActive], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate])
SELECT [ID], [TicketTypeId], [MinDuration], [MaxDuration], [MinAmount], [MaxAmount], [MinOlb], [MaxOlb], [MinArrearsDays], [MaxArrearsDays], [MinArrears], [MaxArrears], [DataRefreshCycle], [DataSyncMode], [LastSyncDate], [NextSyncDate], [IsActive], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate]
FROM [dbo].[TicketTypeLmsConfig];
SET IDENTITY_INSERT [dbo].[TicketTypeLmsConfig] OFF;

-- ========== End of generated script ==========
