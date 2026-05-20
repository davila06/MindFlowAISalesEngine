
export type Locale = "en" | "es";

export type TranslationKey =
  | "app.title"
  | "email.templates.defaultSubject"
  | "email.templates.defaultBodyHtml"
  | "email.templates.defaultRequiredVariables"
  | "app.tenant"
  | "app.menu"
  | "app.skipToContent"
  | "language.label"
  | "language.en"
  | "language.es"
  | "nav.dashboard"
  | "nav.pipeline"
  | "nav.leadActivities"
  | "nav.rules"
  | "nav.emailSmtp"
  | "nav.emailTemplates"
  | "nav.emailLogs"
  | "nav.admin"
  | "nav.uiGuide"
  | "nav.group.crm"
  | "nav.group.automation"
  | "nav.group.comms"
  | "nav.group.admin"
  | "common.refresh"
  | "common.clearFilter"
  | "common.loading"
  | "common.error"
  | "common.errorTitle"
  | "common.empty"
  | "common.undo"
  | "common.cancel"
  | "common.confirm"
  | "common.active"
  | "common.inactive"
  | "common.enabled"
  | "common.locked"
  | "common.status"
  | "common.actions"
  | "dashboard.title"
  | "dashboard.subtitle"
  | "dashboard.daysWindow"
  | "dashboard.leadsPerDay"
  | "dashboard.totalLeads"
  | "dashboard.conversionRate"
  | "dashboard.pipelineValue"
  | "dashboard.noLeads"
  | "dashboard.microcopy"
  | "dashboard.date"
  | "dashboard.toastError"
  | "dashboard.toastRefreshed"
  | "dashboard.count"
  | "pipeline.title"
  | "pipeline.subtitle"
  | "pipeline.quickActions"
  | "pipeline.leadId"
  | "pipeline.leadIdPlaceholder"
  | "pipeline.opportunityTitle"
  | "pipeline.opportunityValue"
  | "pipeline.createOpportunity"
  | "pipeline.moveToStage"
  | "pipeline.bulkActions"
  | "pipeline.bulkMove"
  | "pipeline.savedView"
  | "pipeline.toastCreated"
  | "pipeline.toastMoved"
  | "pipeline.toastBulkMoved"
  | "pipeline.toastError"
  | "pipeline.applyView"
  | "pipeline.selectOpportunity"
  | "pipeline.focusQuickActions"
  | "pipeline.allStages"
  | "pipeline.selectTargetStage"
  | "pipeline.noStages"
  | "pipeline.noOpportunities"
  | "pipeline.stageEmpty"
  | "pipeline.lead"
  | "pipeline.value"
  | "pipeline.defaultTitle"
  | "pipeline.microcopy"
  | "rules.title"
  | "rules.subtitle"
  | "rules.toastActivated"
  | "rules.toastDeactivated"
  | "rules.toastUndo"
  | "rules.toastError"
  | "rules.toastRefreshed"
  | "rules.microcopy"
  | "rules.filter"
  | "rules.filterPlaceholder"
  | "rules.noMatches"
  | "rules.confirmDeactivatePrefix"
  | "rules.confirmDeactivateSuffix"
  | "rules.deactivatedBannerPrefix"
  | "rules.deactivatedBannerSuffix"
  | "rules.activate"
  | "rules.deactivate"
  | "rules.name"
  | "rules.trigger"
  | "email.smtp.title"
  | "email.smtp.subtitle"
  | "email.smtp.formLabel"
  | "email.smtp.host"
  | "email.smtp.port"
  | "email.smtp.username"
  | "email.smtp.password"
  | "email.smtp.passwordHint"
  | "email.smtp.fromEmail"
  | "email.smtp.fromName"
  | "email.smtp.enableSsl"
  | "email.smtp.save"
  | "email.smtp.saved"
  | "email.logs.title"
  | "email.logs.subtitle"
  | "email.logs.filter"
  | "email.logs.filterPlaceholder"
  | "email.logs.noMatches"
  | "email.logs.date"
  | "email.logs.template"
  | "email.logs.recipient"
  | "email.logs.errorColumn"
  | "email.logs.masked"
  | "email.logs.page"
  | "email.logs.pageSize"
  | "email.logs.previous"
  | "email.logs.next"
  | "email.logs.tracking"
  | "email.logs.opened"
  | "email.logs.notOpened"
  | "email.logs.clicked"
  | "email.logs.notClicked"
  | "email.templates.title"
  | "email.templates.subtitle"
  | "email.templates.subjectLabel"
  | "email.templates.bodyLabel"
  | "email.templates.requiredVariablesLabel"
  | "email.templates.requiredVariablesPlaceholder"
  | "email.templates.publishVersion"
  | "email.templates.preview"
  | "email.templates.templateKey"
  | "email.templates.templateSandboxHint"
  | "email.templates.variableColumn"
  | "email.templates.sampleColumn"
  | "email.templates.rollbackVersion"
  | "email.templates.rollback"
  | "email.templates.versionPrefix"
  | "email.templates.publishedSuffix"
  | "email.templates.publishError"
  | "email.templates.previewError"
  | "email.templates.rollbackError"
  | "email.templates.rollbackPrefix"
  | "email.templates.currentVersion"
  | "email.templates.noRequiredVariables"
  | "email.templates.previewSubject"
  | "email.templates.previewBody"
  | "email.templates.rulesOnly"
  | "email.templates.availableVariables"
  | "email.templates.name"
  | "email.templates.type"
  | "rules.builderTitle"
  | "rules.builderSubtitle"
  | "rules.builderLoadTemplates"
  | "rules.builderTemplate"
  | "rules.builderApplyTemplate"
  | "rules.builderName"
  | "rules.builderTrigger"
  | "rules.builderConditionField"
  | "rules.builderConditionOperator"
  | "rules.builderConditionValue"
  | "rules.builderActionType"
  | "rules.builderActionValue"
  | "rules.builderAddCondition"
  | "rules.builderRemoveCondition"
  | "rules.builderAddAction"
  | "rules.builderRemoveAction"
  | "rules.builderCreate"
  | "rules.builderCreated"
  | "rules.builderLoadRule"
  | "rules.builderSaveChanges"
  | "rules.builderUpdated"
  | "rules.builderSelectRule"
  | "rules.builderSimulate"
  | "rules.builderRollbackVersion"
  | "rules.builderRollback"
  | "rules.builderRollbackDone"
  | "rules.builderFixtureMatched"
  | "rules.builderFixtureApplied"
  | "admin.title"
  | "admin.subtitle"
  | "admin.rulesManagement"
  | "admin.smtpConfiguration"
  | "admin.emailLogs"
  | "admin.openUiGuide"
  | "uiGuide.title"
  | "uiGuide.subtitle"
  | "uiGuide.buttons"
  | "uiGuide.primaryAction"
  | "uiGuide.secondaryAction"
  | "uiGuide.destructiveAction"
  | "uiGuide.fields"
  | "uiGuide.sampleInput"
  | "uiGuide.samplePlaceholder"
  | "uiGuide.sampleHint"
  | "uiGuide.feedbackStates"
  | "uiGuide.feedbackAnnounce"
  | "uiGuide.noRecords"
  | "uiGuide.emptyDescription"
  | "uiGuide.errorDescription"
  | "uiGuide.loadingStates"
  | "uiGuide.loadingDescription"
  | "uiGuide.loadingAnnounce"
  | "uiGuide.kpiCards"
  | "leadActivities.title"
  | "leadActivities.subtitle"
  | "leadActivities.leadId"
  | "leadActivities.leadIdPlaceholder"
  | "leadActivities.typeFilter"
  | "leadActivities.pageSize"
  | "leadActivities.page"
  | "leadActivities.empty"
  | "leadActivities.invalidLeadId"
  | "leadActivities.addNote"
  | "leadActivities.notePlaceholder"
  | "leadActivities.addNoteAction"
  | "leadActivities.untitled"
  | "leadActivities.actor"
  | "leadActivities.type.all"
  | "leadActivities.type.lead_created"
  | "leadActivities.type.note_added"
  | "leadActivities.type.email_sent"
  | "leadActivities.type.stage_changed"
  | "leadActivities.type.assigned"
  | "leadActivities.type.score_changed"
  | "leadActivities.type.call_logged"
  | "leadActivities.type.whatsapp_sent"
  | "leadActivities.type.whatsapp_received"
  | "leadActivities.type.sequence_step_sent"
  // Sequences
  | "nav.sequences"
  | "sequences.title"
  | "sequences.subtitle"
  | "sequences.create"
  | "sequences.empty"
  | "sequences.name"
  | "sequences.description"
  | "sequences.isActive"
  | "sequences.steps"
  | "sequences.step.order"
  | "sequences.step.actionType"
  | "sequences.step.actionValue"
  | "sequences.step.delayDays"
  | "sequences.step.addStep"
  | "sequences.enroll"
  | "sequences.enrollLeadId"
  | "sequences.enrollSuccess"
  | "sequences.unenroll"
  | "sequences.save"
  | "sequences.delete"
  | "sequences.activeLabel"
  | "sequences.inactiveLabel"
  // Custom Fields
  | "nav.customFields"
  | "customFields.title"
  | "customFields.subtitle"
  | "customFields.create"
  | "customFields.empty"
  | "customFields.key"
  | "customFields.label"
  | "customFields.fieldType"
  | "customFields.entityType"
  | "customFields.options"
  | "customFields.isRequired"
  | "customFields.order"
  | "customFields.save"
  | "customFields.delete"
  | "customFields.type.text"
  | "customFields.type.number"
  | "customFields.type.date"
  | "customFields.type.select"
  | "customFields.type.boolean"
  // WhatsApp
  | "nav.whatsapp"
  | "whatsapp.title"
  | "whatsapp.subtitle"
  | "whatsapp.sendMessage"
  | "whatsapp.phone"
  | "whatsapp.body"
  | "whatsapp.send"
  | "whatsapp.optIn"
  | "whatsapp.optOut"
  | "whatsapp.conversation"
  | "whatsapp.inbound"
  | "whatsapp.outbound"
  | "whatsapp.status"
  | "whatsapp.empty"
  | "whatsapp.notConfigured"
  // Lead Search
  | "leads.title"
  | "leads.subtitle"
  | "leads.search"
  | "leads.empty"
  | "leads.sortBy"
  | "leads.sortDir"
  | "leads.cfSort"
  | "leads.cfSortDir"
  | "leads.cfFilters"
  | "leads.addFilter"
  | "leads.removeFilter"
  | "leads.applyFilters"
  | "leads.clearFilters"
  | "leads.col.email"
  | "leads.col.phone"
  | "leads.col.source"
  | "leads.col.score"
  | "leads.col.priority"
  | "leads.col.createdAt"
  | "leads.total"
  | "leads.prevPage"
  | "leads.nextPage"
  | "home.title"
  | "home.subtitle"
  | "home.goDashboard";

export const messages: Record<Locale, Record<TranslationKey, string>> = {
  en: {
    "app.title": "MindFlow IA Sales Engine",
    "email.templates.defaultSubject": "Welcome {{lead.name}}",
    "email.templates.defaultBodyHtml": "<p>Hello {{lead.name}}</p><p>Stage: {{pipeline.stage}}</p>",
    "email.templates.defaultRequiredVariables": "lead.name, pipeline.stage",
    "app.tenant": "Tenant",
    "app.menu": "Menu",
    "app.skipToContent": "Skip to content",
    "language.label": "Language",
    "language.en": "English",
    "language.es": "Spanish",
    "nav.dashboard": "Dashboard",
    "nav.pipeline": "Pipeline",
    "nav.leadActivities": "Lead Timeline",
    "nav.rules": "Rules",
    "nav.emailSmtp": "Email SMTP",
    "nav.emailTemplates": "Email Templates",
    "nav.emailLogs": "Email Logs",
    "nav.admin": "Admin",
    "nav.uiGuide": "UI Guide",
    "nav.group.crm": "CRM",
    "nav.group.automation": "Automation",
    "nav.group.comms": "Communications",
    "nav.group.admin": "Administration",
    "common.refresh": "Refresh",
    "common.clearFilter": "Clear filter",
    "common.loading": "Loading",
    "common.error": "Something went wrong",
    "common.errorTitle": "Error",
    "common.empty": "No data available",
    "common.undo": "Undo",
    "common.cancel": "Cancel",
    "common.confirm": "Confirm",
    "common.active": "Active",
    "common.inactive": "Inactive",
    "common.enabled": "Enabled",
    "common.locked": "Locked",
    "common.status": "Status",
    "common.actions": "Actions",
    "dashboard.title": "Dashboard",
    "dashboard.subtitle": "Leads, conversion and pipeline value overview.",
    "dashboard.daysWindow": "Days window",
    "dashboard.leadsPerDay": "Leads Per Day",
    "dashboard.totalLeads": "Total Leads",
    "dashboard.conversionRate": "Conversion Rate",
    "dashboard.pipelineValue": "Pipeline Value",
    "dashboard.noLeads": "No leads were found.",
    "dashboard.microcopy": "Start by adjusting the day window to spot trends before taking action.",
    "dashboard.date": "Date",
    "dashboard.toastError": "Error loading dashboard.",
    "dashboard.toastRefreshed": "Dashboard updated!",
    "dashboard.count": "Count",
    "pipeline.title": "Pipeline Board",
    "pipeline.subtitle": "Kanban operations by stage.",
    "pipeline.quickActions": "Opportunity quick actions",
    "pipeline.leadId": "Lead Id",
    "pipeline.leadIdPlaceholder": "lead-123",
    "pipeline.opportunityTitle": "Title",
    "pipeline.opportunityValue": "Value",
    "pipeline.createOpportunity": "Create Opportunity",
    "pipeline.moveToStage": "Move to stage",
    "pipeline.bulkActions": "Bulk actions",
    "pipeline.bulkMove": "Bulk move",
    "pipeline.savedView": "Saved view",
    "pipeline.toastCreated": "Opportunity created!",
    "pipeline.toastMoved": "Opportunity moved!",
    "pipeline.toastBulkMoved": "Bulk move completed!",
    "pipeline.toastError": "Something went wrong.",
    "pipeline.applyView": "Apply view",
    "pipeline.selectOpportunity": "Select opportunity",
    "pipeline.focusQuickActions": "Focus quick actions",
    "pipeline.allStages": "All stages",
    "pipeline.selectTargetStage": "Select target stage",
    "pipeline.noStages": "No stages configured.",
    "pipeline.noOpportunities": "No opportunities",
    "pipeline.stageEmpty": "Stage is empty.",
    "pipeline.lead": "Lead",
    "pipeline.value": "Value",
    "pipeline.defaultTitle": "New deal",
    "pipeline.microcopy": "Create quickly, then move only qualified opportunities to keep momentum.",
    "rules.title": "Rules Engine",
    "rules.subtitle": "Trigger -> Condition -> Action",
    "rules.toastActivated": "Rule activated!",
    "rules.toastDeactivated": "Rule deactivated!",
    "rules.toastUndo": "Deactivation undone!",
    "rules.toastError": "Error updating rule.",
    "rules.toastRefreshed": "Rules updated!",
    "rules.microcopy": "Filter and activate only rules that are currently relevant to your operation.",
    "rules.filter": "Filter rules",
    "rules.filterPlaceholder": "Search by name or trigger",
    "rules.noMatches": "No rules match current filter.",
    "rules.confirmDeactivatePrefix": "Deactivate rule",
    "rules.confirmDeactivateSuffix": "This can impact automations.",
    "rules.deactivatedBannerPrefix": "Rule",
    "rules.deactivatedBannerSuffix": "deactivated.",
    "rules.activate": "Activate",
    "rules.deactivate": "Deactivate",
    "rules.name": "Name",
    "rules.trigger": "Trigger",
    "email.smtp.title": "SMTP Configuration",
    "email.smtp.subtitle": "Tenant-scoped email delivery settings.",
    "email.smtp.formLabel": "SMTP settings form",
    "email.smtp.host": "Host",
    "email.smtp.port": "Port",
    "email.smtp.username": "Username",
    "email.smtp.password": "Password",
    "email.smtp.passwordHint": "Leave empty to keep current password.",
    "email.smtp.fromEmail": "From Email",
    "email.smtp.fromName": "From Name",
    "email.smtp.enableSsl": "Enable SSL",
    "email.smtp.save": "Save SMTP",
    "email.smtp.saved": "SMTP settings saved.",
    "email.logs.title": "Email Logs",
    "email.logs.subtitle": "Read-only email execution history.",
    "email.logs.filter": "Filter logs",
    "email.logs.filterPlaceholder": "Search template, status or recipient",
    "email.logs.noMatches": "No email logs found for this filter.",
    "email.logs.date": "Date",
    "email.logs.template": "Template",
    "email.logs.recipient": "Recipient",
    "email.logs.errorColumn": "Error",
    "email.logs.masked": "masked",
    "email.logs.page": "Page",
    "email.logs.pageSize": "Page size",
    "email.logs.previous": "Previous",
    "email.logs.next": "Next",
    "email.logs.tracking": "Tracking",
    "email.logs.opened": "Opened",
    "email.logs.notOpened": "Not opened",
    "email.logs.clicked": "Clicked",
    "email.logs.notClicked": "Not clicked",
    "email.templates.title": "Email Templates",
    "email.templates.subtitle": "Templates are automation assets and are executed by rules.",
    "email.templates.subjectLabel": "Subject",
    "email.templates.bodyLabel": "Body HTML",
    "email.templates.requiredVariablesLabel": "Required Variables",
    "email.templates.requiredVariablesPlaceholder": "lead.name, pipeline.stage",
    "email.templates.publishVersion": "Publish version",
    "email.templates.preview": "Preview",
    "email.templates.templateKey": "Template key",
    "email.templates.templateSandboxHint": "Versioned admin sandbox for the live welcome email.",
    "email.templates.variableColumn": "Variable",
    "email.templates.sampleColumn": "Sample",
    "email.templates.rollbackVersion": "Rollback version",
    "email.templates.rollback": "Rollback",
    "email.templates.versionPrefix": "Version",
    "email.templates.publishedSuffix": "published.",
    "email.templates.publishError": "Failed to publish template version",
    "email.templates.previewError": "Failed to preview template",
    "email.templates.rollbackError": "Failed to rollback template",
    "email.templates.rollbackPrefix": "Rolled back to version",
    "email.templates.currentVersion": "Current version",
    "email.templates.noRequiredVariables": "No required variables",
    "email.templates.previewSubject": "Preview Subject",
    "email.templates.previewBody": "Preview Body",
    "email.templates.rulesOnly": "Templates are triggered by Rules only.",
    "email.templates.availableVariables": "Available Variables",
    "email.templates.name": "Name",
    "email.templates.type": "Type",
    "rules.builderTitle": "Guided Rule Builder",
    "rules.builderSubtitle": "Create, simulate fixture and rollback rules without manual JSON editing.",
    "rules.builderLoadTemplates": "Load templates",
    "rules.builderTemplate": "Template",
    "rules.builderApplyTemplate": "Apply template",
    "rules.builderName": "Rule name",
    "rules.builderTrigger": "Trigger",
    "rules.builderConditionField": "Condition field",
    "rules.builderConditionOperator": "Condition operator",
    "rules.builderConditionValue": "Condition value",
    "rules.builderActionType": "Action type",
    "rules.builderActionValue": "Action value",
    "rules.builderAddCondition": "Add condition",
    "rules.builderRemoveCondition": "Remove condition",
    "rules.builderAddAction": "Add action",
    "rules.builderRemoveAction": "Remove action",
    "rules.builderCreate": "Create rule",
    "rules.builderCreated": "Rule created successfully.",
    "rules.builderLoadRule": "Load rule",
    "rules.builderSaveChanges": "Save changes",
    "rules.builderUpdated": "Rule updated.",
    "rules.builderSelectRule": "Select rule",
    "rules.builderSimulate": "Simulate fixture",
    "rules.builderRollbackVersion": "Rollback version",
    "rules.builderRollback": "Rollback",
    "rules.builderRollbackDone": "Rollback applied successfully.",
    "rules.builderFixtureMatched": "Matched",
    "rules.builderFixtureApplied": "Applied",
    "admin.title": "Admin",
    "admin.subtitle": "Tenant administration and access policy checkpoint.",
    "admin.rulesManagement": "Rules Management",
    "admin.smtpConfiguration": "SMTP Configuration",
    "admin.emailLogs": "Email Logs",
    "admin.openUiGuide": "Open UI Pattern Guide",
    "uiGuide.title": "UI Pattern Guide",
    "uiGuide.subtitle": "Enterprise reference for reusable UI patterns, states, and action controls.",
    "uiGuide.buttons": "Buttons",
    "uiGuide.primaryAction": "Primary action",
    "uiGuide.secondaryAction": "Secondary action",
    "uiGuide.destructiveAction": "Destructive action",
    "uiGuide.fields": "Fields",
    "uiGuide.sampleInput": "Sample Input",
    "uiGuide.samplePlaceholder": "Type something...",
    "uiGuide.sampleHint": "Use explicit labels for AA compliance.",
    "uiGuide.feedbackStates": "Feedback states",
    "uiGuide.feedbackAnnounce": "Feedback status",
    "uiGuide.noRecords": "No records",
    "uiGuide.emptyDescription": "This is the standard empty-state pattern.",
    "uiGuide.errorDescription": "This is the standard error-state pattern.",
    "uiGuide.loadingStates": "Loading states",
    "uiGuide.loadingDescription": "Placeholder blocks represent pending content while data is being fetched.",
    "uiGuide.loadingAnnounce": "Loading content",
    "uiGuide.kpiCards": "KPI cards",
    "leadActivities.title": "Lead Activity Timeline",
    "leadActivities.subtitle": "Track lead interactions, automation events, and manual notes.",
    "leadActivities.leadId": "Lead Id",
    "leadActivities.leadIdPlaceholder": "Paste a lead GUID",
    "leadActivities.typeFilter": "Activity type",
    "leadActivities.pageSize": "Page size",
    "leadActivities.page": "Page",
    "leadActivities.empty": "No activities found for this lead.",
    "leadActivities.invalidLeadId": "Enter a valid lead GUID to load timeline activities.",
    "leadActivities.addNote": "Manual note",
    "leadActivities.notePlaceholder": "Add context for sales or support handoff...",
    "leadActivities.addNoteAction": "Add note",
    "leadActivities.untitled": "Activity",
    "leadActivities.actor": "Actor",
    "leadActivities.type.all": "All",
    "leadActivities.type.lead_created": "Lead created",
    "leadActivities.type.note_added": "Note added",
    "leadActivities.type.email_sent": "Email sent",
    "leadActivities.type.stage_changed": "Stage changed",
    "leadActivities.type.assigned": "Assigned",
    "leadActivities.type.score_changed": "Score changed",
    "leadActivities.type.call_logged": "Call logged",
    "leadActivities.type.whatsapp_sent": "WhatsApp sent",
    "leadActivities.type.whatsapp_received": "WhatsApp received",
    "leadActivities.type.sequence_step_sent": "Sequence step sent",
    // Sequences
    "nav.sequences": "Sequences",
    "sequences.title": "Sales Sequences",
    "sequences.subtitle": "Build automated cadences for lead nurturing",
    "sequences.create": "New Sequence",
    "sequences.empty": "No sequences yet. Create your first cadence.",
    "sequences.name": "Name",
    "sequences.description": "Description",
    "sequences.isActive": "Active",
    "sequences.steps": "Steps",
    "sequences.step.order": "Order",
    "sequences.step.actionType": "Action",
    "sequences.step.actionValue": "Value (template name / text)",
    "sequences.step.delayDays": "Delay (days)",
    "sequences.step.addStep": "Add Step",
    "sequences.enroll": "Enroll Lead",
    "sequences.enrollLeadId": "Lead ID",
    "sequences.enrollSuccess": "Lead enrolled successfully",
    "sequences.unenroll": "Exit Enrollment",
    "sequences.save": "Save Sequence",
    "sequences.delete": "Delete",
    "sequences.activeLabel": "Active",
    "sequences.inactiveLabel": "Inactive",
    // Custom Fields
    "nav.customFields": "Custom Fields",
    "customFields.title": "Custom Fields",
    "customFields.subtitle": "Define tenant-specific fields for leads and contacts",
    "customFields.create": "New Field",
    "customFields.empty": "No custom fields defined yet.",
    "customFields.key": "Key (slug)",
    "customFields.label": "Label",
    "customFields.fieldType": "Field Type",
    "customFields.entityType": "Entity",
    "customFields.options": "Options (comma-separated, for select)",
    "customFields.isRequired": "Required",
    "customFields.order": "Display Order",
    "customFields.save": "Save Field",
    "customFields.delete": "Delete",
    "customFields.type.text": "Text",
    "customFields.type.number": "Number",
    "customFields.type.date": "Date",
    "customFields.type.select": "Select",
    "customFields.type.boolean": "Boolean",
    // WhatsApp
    "nav.whatsapp": "WhatsApp",
    "whatsapp.title": "WhatsApp",
    "whatsapp.subtitle": "Manage conversations and opt-in contacts",
    "whatsapp.sendMessage": "Send Message",
    "whatsapp.phone": "Phone (e.g. 15551234567)",
    "whatsapp.body": "Message",
    "whatsapp.send": "Send",
    "whatsapp.optIn": "Opt In",
    "whatsapp.optOut": "Opt Out",
    "whatsapp.conversation": "Conversation",
    "whatsapp.inbound": "Inbound",
    "whatsapp.outbound": "Outbound",
    "whatsapp.status": "Status",
    "whatsapp.empty": "No messages yet.",
    "whatsapp.notConfigured": "WhatsApp is not configured. Set WHATSAPP_PHONE_NUMBER_ID and WHATSAPP_ACCESS_TOKEN.",
    "leads.title": "Leads",
    "leads.subtitle": "Search and filter leads, including by custom field values.",
    "leads.search": "Search",
    "leads.empty": "No leads found.",
    "leads.sortBy": "Sort by",
    "leads.sortDir": "Direction",
    "leads.cfSort": "Sort by custom field",
    "leads.cfSortDir": "Custom field sort direction",
    "leads.cfFilters": "Custom field filters",
    "leads.addFilter": "Add filter",
    "leads.removeFilter": "Remove",
    "leads.applyFilters": "Apply",
    "leads.clearFilters": "Clear all",
    "leads.col.email": "Email",
    "leads.col.phone": "Phone",
    "leads.col.source": "Source",
    "leads.col.score": "Score",
    "leads.col.priority": "Priority",
    "leads.col.createdAt": "Created",
    "leads.total": "Total",
    "leads.prevPage": "Previous",
    "leads.nextPage": "Next",
    "home.title": "MindFlow Frontend",
    "home.subtitle": "This project follows the ARQ-FRONTEND Next.js feature-based structure.",
    "home.goDashboard": "Go to Dashboard"
  },
  es: {
    "app.title": "MindFlow IA Sales Engine",
    "email.templates.defaultSubject": "Bienvenido {{lead.name}}",
    "email.templates.defaultBodyHtml": "<p>Hola {{lead.name}}</p><p>Etapa: {{pipeline.stage}}</p>",
    "email.templates.defaultRequiredVariables": "lead.name, pipeline.stage",
    "app.tenant": "Tenant",
    "app.menu": "Menu",
    "app.skipToContent": "Saltar al contenido",
    "language.label": "Idioma",
    "language.en": "Ingles",
    "language.es": "Espanol",
    "nav.dashboard": "Dashboard",
    "nav.pipeline": "Pipeline",
    "nav.leadActivities": "Timeline de Leads",
    "nav.rules": "Reglas",
    "nav.emailSmtp": "SMTP Email",
    "nav.emailTemplates": "Plantillas Email",
    "nav.emailLogs": "Logs Email",
    "nav.admin": "Admin",
    "nav.uiGuide": "Guia UI",
    "nav.group.crm": "CRM",
    "nav.group.automation": "Automatización",
    "nav.group.comms": "Comunicaciones",
    "nav.group.admin": "Administración",
    "common.refresh": "Refrescar",
    "common.clearFilter": "Limpiar filtro",
    "common.loading": "Cargando",
    "common.error": "Ocurrio un error",
    "common.errorTitle": "Error",
    "common.empty": "No hay datos disponibles",
    "common.undo": "Deshacer",
    "common.cancel": "Cancelar",
    "common.confirm": "Confirmar",
    "common.active": "Activo",
    "common.inactive": "Inactivo",
    "common.enabled": "Habilitado",
    "common.locked": "Bloqueado",
    "common.status": "Estado",
    "common.actions": "Acciones",
    "dashboard.title": "Dashboard",
    "dashboard.subtitle": "Resumen de leads, conversion y valor de pipeline.",
    "dashboard.daysWindow": "Ventana de dias",
    "dashboard.leadsPerDay": "Leads por dia",
    "dashboard.totalLeads": "Leads totales",
    "dashboard.conversionRate": "Tasa de conversion",
    "dashboard.pipelineValue": "Valor del pipeline",
    "dashboard.noLeads": "No se encontraron leads.",
    "dashboard.microcopy": "Ajusta la ventana de dias para detectar tendencias antes de tomar accion.",
    "dashboard.date": "Fecha",
    "dashboard.toastError": "Error al cargar dashboard.",
    "dashboard.toastRefreshed": "¡Dashboard actualizado!",
    "dashboard.count": "Cantidad",
    "pipeline.title": "Tablero Pipeline",
    "pipeline.subtitle": "Operacion Kanban por etapa.",
    "pipeline.quickActions": "Acciones rapidas de oportunidades",
    "pipeline.leadId": "ID de lead",
    "pipeline.leadIdPlaceholder": "lead-123",
    "pipeline.opportunityTitle": "Titulo",
    "pipeline.opportunityValue": "Valor",
    "pipeline.createOpportunity": "Crear oportunidad",
    "pipeline.moveToStage": "Mover a etapa",
    "pipeline.bulkActions": "Acciones masivas",
    "pipeline.bulkMove": "Mover en lote",
    "pipeline.savedView": "Vista guardada",
    "pipeline.toastCreated": "¡Oportunidad creada!",
    "pipeline.toastMoved": "¡Oportunidad movida!",
    "pipeline.toastBulkMoved": "¡Movimiento masivo completado!",
    "pipeline.toastError": "Ocurrió un error.",
    "pipeline.applyView": "Aplicar vista",
    "pipeline.selectOpportunity": "Seleccionar oportunidad",
    "pipeline.focusQuickActions": "Enfocar acciones rapidas",
    "pipeline.allStages": "Todas las etapas",
    "pipeline.selectTargetStage": "Seleccionar etapa destino",
    "pipeline.noStages": "No hay etapas configuradas.",
    "pipeline.noOpportunities": "Sin oportunidades",
    "pipeline.stageEmpty": "La etapa esta vacia.",
    "pipeline.lead": "Lead",
    "pipeline.value": "Valor",
    "pipeline.defaultTitle": "Nuevo deal",
    "pipeline.microcopy": "Crea rapido y mueve solo oportunidades calificadas para mantener ritmo.",
    "rules.title": "Motor de Reglas",
    "rules.subtitle": "Trigger -> Condition -> Action",
    "rules.toastActivated": "¡Regla activada!",
    "rules.toastDeactivated": "¡Regla desactivada!",
    "rules.toastUndo": "¡Desactivación revertida!",
    "rules.toastError": "Error al actualizar regla.",
    "rules.toastRefreshed": "¡Reglas actualizadas!",
    "rules.microcopy": "Filtra y activa solo reglas relevantes para la operacion actual.",
    "rules.filter": "Filtrar reglas",
    "rules.filterPlaceholder": "Buscar por nombre o trigger",
    "rules.noMatches": "No hay reglas para el filtro actual.",
    "rules.confirmDeactivatePrefix": "Desactivar regla",
    "rules.confirmDeactivateSuffix": "Esto puede impactar automatizaciones.",
    "rules.deactivatedBannerPrefix": "Regla",
    "rules.deactivatedBannerSuffix": "desactivada.",
    "rules.activate": "Activar",
    "rules.deactivate": "Desactivar",
    "rules.name": "Nombre",
    "rules.trigger": "Trigger",
    "email.smtp.title": "Configuracion SMTP",
    "email.smtp.subtitle": "Configuracion de envio por tenant.",
    "email.smtp.formLabel": "Formulario de configuracion SMTP",
    "email.smtp.host": "Host",
    "email.smtp.port": "Puerto",
    "email.smtp.username": "Usuario",
    "email.smtp.password": "Contrasena",
    "email.smtp.passwordHint": "Dejalo vacio para mantener la contrasena actual.",
    "email.smtp.fromEmail": "Email remitente",
    "email.smtp.fromName": "Nombre remitente",
    "email.smtp.enableSsl": "Habilitar SSL",
    "email.smtp.save": "Guardar SMTP",
    "email.smtp.saved": "Configuracion SMTP guardada.",
    "email.logs.title": "Logs de Email",
    "email.logs.subtitle": "Historial de ejecucion de emails en modo lectura.",
    "email.logs.filter": "Filtrar logs",
    "email.logs.filterPlaceholder": "Buscar plantilla, estado o destinatario",
    "email.logs.noMatches": "No hay logs para este filtro.",
    "email.logs.date": "Fecha",
    "email.logs.template": "Plantilla",
    "email.logs.recipient": "Destinatario",
    "email.logs.errorColumn": "Error",
    "email.logs.masked": "oculto",
    "email.logs.page": "Pagina",
    "email.logs.pageSize": "Tamano de pagina",
    "email.logs.previous": "Anterior",
    "email.logs.next": "Siguiente",
    "email.logs.tracking": "Rastreo",
    "email.logs.opened": "Abierto",
    "email.logs.notOpened": "No abierto",
    "email.logs.clicked": "Clic",
    "email.logs.notClicked": "Sin clic",
    "email.templates.title": "Plantillas de Email",
    "email.templates.subtitle": "Las plantillas son activos de automatizacion y se ejecutan por reglas.",
    "email.templates.subjectLabel": "Asunto",
    "email.templates.bodyLabel": "HTML del cuerpo",
    "email.templates.requiredVariablesLabel": "Variables requeridas",
    "email.templates.requiredVariablesPlaceholder": "lead.name, pipeline.stage",
    "email.templates.publishVersion": "Publicar version",
    "email.templates.preview": "Previsualizar",
    "email.templates.templateKey": "Clave de plantilla",
    "email.templates.templateSandboxHint": "Sandbox admin versionado para el email welcome activo.",
    "email.templates.variableColumn": "Variable",
    "email.templates.sampleColumn": "Muestra",
    "email.templates.rollbackVersion": "Version para rollback",
    "email.templates.rollback": "Rollback",
    "email.templates.versionPrefix": "Version",
    "email.templates.publishedSuffix": "publicada.",
    "email.templates.publishError": "No se pudo publicar la version de plantilla",
    "email.templates.previewError": "No se pudo generar el preview",
    "email.templates.rollbackError": "No se pudo ejecutar rollback",
    "email.templates.rollbackPrefix": "Rollback aplicado a version",
    "email.templates.currentVersion": "Version actual",
    "email.templates.noRequiredVariables": "Sin variables requeridas",
    "email.templates.previewSubject": "Preview de asunto",
    "email.templates.previewBody": "Preview de cuerpo",
    "email.templates.rulesOnly": "Las plantillas se disparan solo por Reglas.",
    "email.templates.availableVariables": "Variables disponibles",
    "email.templates.name": "Nombre",
    "email.templates.type": "Tipo",
    "rules.builderTitle": "Constructor Guiado de Reglas",
    "rules.builderSubtitle": "Crea, simula fixtures y aplica rollback sin editar JSON manual.",
    "rules.builderLoadTemplates": "Cargar plantillas",
    "rules.builderTemplate": "Plantilla",
    "rules.builderApplyTemplate": "Aplicar plantilla",
    "rules.builderName": "Nombre de regla",
    "rules.builderTrigger": "Trigger",
    "rules.builderConditionField": "Campo de condicion",
    "rules.builderConditionOperator": "Operador de condicion",
    "rules.builderConditionValue": "Valor de condicion",
    "rules.builderActionType": "Tipo de accion",
    "rules.builderActionValue": "Valor de accion",
    "rules.builderAddCondition": "Agregar condicion",
    "rules.builderRemoveCondition": "Quitar condicion",
    "rules.builderAddAction": "Agregar accion",
    "rules.builderRemoveAction": "Quitar accion",
    "rules.builderCreate": "Crear regla",
    "rules.builderCreated": "Regla creada correctamente.",
    "rules.builderLoadRule": "Cargar regla",
    "rules.builderSaveChanges": "Guardar cambios",
    "rules.builderUpdated": "Regla actualizada.",
    "rules.builderSelectRule": "Seleccionar regla",
    "rules.builderSimulate": "Simular fixture",
    "rules.builderRollbackVersion": "Version de rollback",
    "rules.builderRollback": "Aplicar rollback",
    "rules.builderRollbackDone": "Rollback aplicado correctamente.",
    "rules.builderFixtureMatched": "Coincidio",
    "rules.builderFixtureApplied": "Aplicado",
    "admin.title": "Admin",
    "admin.subtitle": "Punto de control para administracion de tenant y politicas de acceso.",
    "admin.rulesManagement": "Gestion de reglas",
    "admin.smtpConfiguration": "Configuracion SMTP",
    "admin.emailLogs": "Logs de Email",
    "admin.openUiGuide": "Abrir guia de patrones UI",
    "uiGuide.title": "Guia de Patrones UI",
    "uiGuide.subtitle": "Referencia enterprise de patrones UI reutilizables, estados y controles de accion.",
    "uiGuide.buttons": "Botones",
    "uiGuide.primaryAction": "Accion primaria",
    "uiGuide.secondaryAction": "Accion secundaria",
    "uiGuide.destructiveAction": "Accion destructiva",
    "uiGuide.fields": "Campos",
    "uiGuide.sampleInput": "Input de ejemplo",
    "uiGuide.samplePlaceholder": "Escribe algo...",
    "uiGuide.sampleHint": "Usa labels explicitos para cumplimiento AA.",
    "uiGuide.feedbackStates": "Estados de feedback",
    "uiGuide.feedbackAnnounce": "Estado de retroalimentacion",
    "uiGuide.noRecords": "Sin registros",
    "uiGuide.emptyDescription": "Este es el patron estandar de estado vacio.",
    "uiGuide.errorDescription": "Este es el patron estandar de estado de error.",
    "uiGuide.loadingStates": "Estados de carga",
    "uiGuide.loadingDescription": "Los bloques placeholder representan contenido pendiente mientras se consulta la data.",
    "uiGuide.loadingAnnounce": "Cargando contenido",
    "uiGuide.kpiCards": "Tarjetas KPI",
    "leadActivities.title": "Timeline de Actividad del Lead",
    "leadActivities.subtitle": "Seguimiento de interacciones, eventos automáticos y notas manuales.",
    "leadActivities.leadId": "ID de lead",
    "leadActivities.leadIdPlaceholder": "Pega un GUID de lead",
    "leadActivities.typeFilter": "Tipo de actividad",
    "leadActivities.pageSize": "Tamano de pagina",
    "leadActivities.page": "Pagina",
    "leadActivities.empty": "No hay actividades para este lead.",
    "leadActivities.invalidLeadId": "Ingresa un GUID de lead valido para cargar la timeline.",
    "leadActivities.addNote": "Nota manual",
    "leadActivities.notePlaceholder": "Agrega contexto para ventas o handoff de soporte...",
    "leadActivities.addNoteAction": "Agregar nota",
    "leadActivities.untitled": "Actividad",
    "leadActivities.actor": "Actor",
    "leadActivities.type.all": "Todas",
    "leadActivities.type.lead_created": "Lead creado",
    "leadActivities.type.note_added": "Nota agregada",
    "leadActivities.type.email_sent": "Email enviado",
    "leadActivities.type.stage_changed": "Etapa cambiada",
    "leadActivities.type.assigned": "Asignado",
    "leadActivities.type.score_changed": "Score actualizado",
    "leadActivities.type.call_logged": "Llamada registrada",
    "leadActivities.type.whatsapp_sent": "WhatsApp enviado",
    "leadActivities.type.whatsapp_received": "WhatsApp recibido",
    "leadActivities.type.sequence_step_sent": "Paso de secuencia enviado",
    // Sequences
    "nav.sequences": "Secuencias",
    "sequences.title": "Secuencias de Ventas",
    "sequences.subtitle": "Construye cadencias automatizadas para la captación de leads",
    "sequences.create": "Nueva Secuencia",
    "sequences.empty": "Sin secuencias aún. Crea tu primera cadencia.",
    "sequences.name": "Nombre",
    "sequences.description": "Descripción",
    "sequences.isActive": "Activa",
    "sequences.steps": "Pasos",
    "sequences.step.order": "Orden",
    "sequences.step.actionType": "Acción",
    "sequences.step.actionValue": "Valor (nombre de template / texto)",
    "sequences.step.delayDays": "Demora (días)",
    "sequences.step.addStep": "Agregar Paso",
    "sequences.enroll": "Inscribir Lead",
    "sequences.enrollLeadId": "ID del Lead",
    "sequences.enrollSuccess": "Lead inscrito exitosamente",
    "sequences.unenroll": "Salir de Inscripción",
    "sequences.save": "Guardar Secuencia",
    "sequences.delete": "Eliminar",
    "sequences.activeLabel": "Activa",
    "sequences.inactiveLabel": "Inactiva",
    // Custom Fields
    "nav.customFields": "Campos Personalizados",
    "customFields.title": "Campos Personalizados",
    "customFields.subtitle": "Define campos específicos del tenant para leads y contactos",
    "customFields.create": "Nuevo Campo",
    "customFields.empty": "Sin campos personalizados definidos aún.",
    "customFields.key": "Clave (slug)",
    "customFields.label": "Etiqueta",
    "customFields.fieldType": "Tipo de Campo",
    "customFields.entityType": "Entidad",
    "customFields.options": "Opciones (separadas por coma, para select)",
    "customFields.isRequired": "Requerido",
    "customFields.order": "Orden de Visualización",
    "customFields.save": "Guardar Campo",
    "customFields.delete": "Eliminar",
    "customFields.type.text": "Texto",
    "customFields.type.number": "Número",
    "customFields.type.date": "Fecha",
    "customFields.type.select": "Selección",
    "customFields.type.boolean": "Booleano",
    // WhatsApp
    "nav.whatsapp": "WhatsApp",
    "whatsapp.title": "WhatsApp",
    "whatsapp.subtitle": "Gestiona conversaciones y contactos con opt-in",
    "whatsapp.sendMessage": "Enviar Mensaje",
    "whatsapp.phone": "Teléfono (ej. 15551234567)",
    "whatsapp.body": "Mensaje",
    "whatsapp.send": "Enviar",
    "whatsapp.optIn": "Opt In",
    "whatsapp.optOut": "Opt Out",
    "whatsapp.conversation": "Conversación",
    "whatsapp.inbound": "Entrante",
    "whatsapp.outbound": "Saliente",
    "whatsapp.status": "Estado",
    "whatsapp.empty": "Sin mensajes aún.",
    "whatsapp.notConfigured": "WhatsApp no está configurado. Establece WHATSAPP_PHONE_NUMBER_ID y WHATSAPP_ACCESS_TOKEN.",
    "leads.title": "Leads",
    "leads.subtitle": "Busca y filtra leads, incluyendo por valores de campos personalizados.",
    "leads.search": "Buscar",
    "leads.empty": "No se encontraron leads.",
    "leads.sortBy": "Ordenar por",
    "leads.sortDir": "Dirección",
    "leads.cfSort": "Ordenar por campo personalizado",
    "leads.cfSortDir": "Dirección campo personalizado",
    "leads.cfFilters": "Filtros de campos personalizados",
    "leads.addFilter": "Agregar filtro",
    "leads.removeFilter": "Eliminar",
    "leads.applyFilters": "Aplicar",
    "leads.clearFilters": "Limpiar todo",
    "leads.col.email": "Email",
    "leads.col.phone": "Teléfono",
    "leads.col.source": "Fuente",
    "leads.col.score": "Puntaje",
    "leads.col.priority": "Prioridad",
    "leads.col.createdAt": "Creado",
    "leads.total": "Total",
    "leads.prevPage": "Anterior",
    "leads.nextPage": "Siguiente",
    "home.title": "MindFlow Frontend",
    "home.subtitle": "Este proyecto sigue la estructura feature-based ARQ-FRONTEND para Next.js.",
    "home.goDashboard": "Ir al Dashboard"
  }
};
