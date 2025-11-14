{{- define "acr_library.dotnet_monitor.env" -}}
- name: DOTNET_DiagnosticPorts
  value: /diag/dotnet-monitor.sock,nosuspend
{{- end -}}

{{- define "acr_library.dotnet_monitor.volume" -}}
- name: diagvol
  emptyDir: {}
{{- end -}}

{{- define "acr_library.dotnet_monitor.volumeMount" -}}
- name: diagvol
  mountPath: /diag
{{- end -}}
