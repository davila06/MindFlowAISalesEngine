export type Locale = "en" | "es";

export type TranslationKey =
  | "app.title"
  | "app.tenant"
  | "app.skipToContent"
  | "language.label"
  | "language.en"
  | "language.es"
  | "nav.dashboard"
  | "nav.pipeline"
  | "nav.rules"
  | "nav.emailSmtp"
  | "nav.emailTemplates"
  | "nav.emailLogs"
  | "nav.admin"
  | "nav.uiGuide"
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
  | "dashboard.date"
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
  | "rules.title"
  | "rules.subtitle"
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
  | "uiGuide.noRecords"
  | "uiGuide.emptyDescription"
  | "uiGuide.errorDescription"
  | "uiGuide.loadingStates"
  | "uiGuide.kpiCards"
  | "home.title"
  | "home.subtitle"
  | "home.goDashboard";

export const messages: Record<Locale, Record<TranslationKey, string>> = {
  en: {
    "app.title": "MindFlow",
    "app.tenant": "Tenant",
    "app.skipToContent": "Skip to content",
    "language.label": "Language",
    "language.en": "English",
    "language.es": "Spanish",
    "nav.dashboard": "Dashboard",
    "nav.pipeline": "Pipeline",
    "nav.rules": "Rules",
    "nav.emailSmtp": "Email SMTP",
    "nav.emailTemplates": "Email Templates",
    "nav.emailLogs": "Email Logs",
    "nav.admin": "Admin",
    "nav.uiGuide": "UI Guide",
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
    "dashboard.date": "Date",
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
    "rules.title": "Rules Engine",
    "rules.subtitle": "Trigger -> Condition -> Action",
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
    "uiGuide.noRecords": "No records",
    "uiGuide.emptyDescription": "This is the standard empty-state pattern.",
    "uiGuide.errorDescription": "This is the standard error-state pattern.",
    "uiGuide.loadingStates": "Loading states",
    "uiGuide.kpiCards": "KPI cards",
    "home.title": "MindFlow Frontend",
    "home.subtitle": "This project follows the ARQ-FRONTEND Next.js feature-based structure.",
    "home.goDashboard": "Go to Dashboard"
  },
  es: {
    "app.title": "MindFlow",
    "app.tenant": "Tenant",
    "app.skipToContent": "Saltar al contenido",
    "language.label": "Idioma",
    "language.en": "Ingles",
    "language.es": "Espanol",
    "nav.dashboard": "Dashboard",
    "nav.pipeline": "Pipeline",
    "nav.rules": "Reglas",
    "nav.emailSmtp": "SMTP Email",
    "nav.emailTemplates": "Plantillas Email",
    "nav.emailLogs": "Logs Email",
    "nav.admin": "Admin",
    "nav.uiGuide": "Guia UI",
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
    "dashboard.date": "Fecha",
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
    "rules.title": "Motor de Reglas",
    "rules.subtitle": "Trigger -> Condition -> Action",
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
    "uiGuide.noRecords": "Sin registros",
    "uiGuide.emptyDescription": "Este es el patron estandar de estado vacio.",
    "uiGuide.errorDescription": "Este es el patron estandar de estado de error.",
    "uiGuide.loadingStates": "Estados de carga",
    "uiGuide.kpiCards": "Tarjetas KPI",
    "home.title": "MindFlow Frontend",
    "home.subtitle": "Este proyecto sigue la estructura feature-based ARQ-FRONTEND para Next.js.",
    "home.goDashboard": "Ir al Dashboard"
  }
};
