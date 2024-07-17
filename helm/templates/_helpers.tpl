{{/*
Expand the name of the chart.
*/}}
{{- define "q-copilot.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
*/}}
{{- define "q-copilot.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- if .Values.nameOverride }}
{{- .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- .Chart.Name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create sub app name and version as used by the chart label.
*/}}
{{- define "q-copilot.webapp.fullname" -}}
{{- printf "%s-%s" (include "q-copilot.fullname" .) .Values.webapp.name | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "q-copilot.webapi.fullname" -}}
{{- printf "%s-%s" (include "q-copilot.fullname" .) .Values.webapi.name | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "q-copilot.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "q-copilot.labels" -}}
helm.sh/chart: {{ include "q-copilot.chart" . }}
{{ include "q-copilot.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{- define "q-copilot.webapp.labels" -}}
helm.sh/chart: {{ include "q-copilot.chart" . }}
{{ include "q-copilot.webapp.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{- define "q-copilot.webapi.labels" -}}
helm.sh/chart: {{ include "q-copilot.chart" . }}
{{ include "q-copilot.webapi.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}


{{/*
Selector labels
*/}}
{{- define "q-copilot.selectorLabels" -}}
app.kubernetes.io/name: {{ .Values.name }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}
{{- define "q-copilot.webapp.selectorLabels" -}}
app.kubernetes.io/name: {{ .Values.webapp.name }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{- define "q-copilot.webapi.selectorLabels" -}}
app.kubernetes.io/name: {{ .Values.webapi.name }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}


{{/*
Create the name of the service account to use
*/}}
{{- define "q-copilot.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "q-copilot.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{/*
Configmap generation
*/}}
{{- define "q-copilot.webapp.configs" -}}
data:
{{ range $v :=  .Values.webapp.configs }}
{{ $v | regexFind "([^/]+$)" | indent 2 }}: |- {{ $.Files.Get $v | nindent 4 }}
{{ end }}
{{ range $v :=  .Values.fe.configs }}
{{ $v | regexFind "([^/]+$)" | indent 2 }}: |- {{ $.Files.Get $v | nindent 4 }}
{{ end }}
{{ range $v :=  .Values.configs }}
{{ $v | regexFind "([^/]+$)" | indent 2 }}: |- {{ $.Files.Get $v | nindent 4 }}
{{ end }}
{{- end }}

{{- define "q-copilot.webapi.configs" -}}
data:
{{ range $v :=  .Values.webapi.configs }}
{{ $v | regexFind "([^/]+$)" | indent 2 }}: |- {{ $.Files.Get $v | nindent 4 }}
{{ end }}
{{ range $v :=  .Values.fe.configs }}
{{ $v | regexFind "([^/]+$)" | indent 2 }}: |- {{ $.Files.Get $v | nindent 4 }}
{{ end }}
{{ range $v :=  .Values.configs }}
{{ $v | regexFind "([^/]+$)" | indent 2 }}: |- {{ $.Files.Get $v | nindent 4 }}
{{ end }}
{{- end }}
