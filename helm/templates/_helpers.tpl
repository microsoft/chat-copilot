{{/*
Expand the name of the chart.
*/}}
{{- define "q-pilot.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
*/}}
{{- define "q-pilot.fullname" -}}
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
{{- define "q-pilot.webapp.fullname" -}}
{{- printf "%s-%s" (include "q-pilot.fullname" .) .Values.webapp.name | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "q-pilot.webapi.fullname" -}}
{{- printf "%s-%s" (include "q-pilot.fullname" .) .Values.webapi.name | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "q-pilot.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "q-pilot.labels" -}}
helm.sh/chart: {{ include "q-pilot.chart" . }}
{{ include "q-pilot.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{- define "q-pilot.webapp.labels" -}}
helm.sh/chart: {{ include "q-pilot.chart" . }}
{{ include "q-pilot.webapp.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{- define "q-pilot.webapi.labels" -}}
helm.sh/chart: {{ include "q-pilot.chart" . }}
{{ include "q-pilot.webapi.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}


{{/*
Selector labels
*/}}
{{- define "q-pilot.selectorLabels" -}}
app.kubernetes.io/name: {{ .Values.name }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}
{{- define "q-pilot.webapp.selectorLabels" -}}
app.kubernetes.io/name: {{ .Values.webapp.name }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{- define "q-pilot.webapi.selectorLabels" -}}
app.kubernetes.io/name: {{ .Values.webapi.name }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}


{{/*
Create the name of the service account to use
*/}}
{{- define "q-pilot.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "q-pilot.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{/*
Configmap generation
*/}}
{{- define "q-pilot.webapp.configs" -}}
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

{{- define "q-pilot.webapi.configs" -}}
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
