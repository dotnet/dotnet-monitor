### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fswagger-ui)

# Swagger API Explorer

dotnet-monitor includes the Swagger UI for exploring the API surface of dotnet-monitor. It can be accessed from the /swagger path (and at the time of writing, will also be redirected to from the "/" path ). The API explorer enables you to see the API endpoints and try them directly from the browser.

If dotnet-monitor is configured to use API Key authentication, then JWT token required to access the service can be supplied by clicking on the Authorize button at top right of the page, and pasting the token text into the popup dialog.

## Known Limitations

The swagger API explorer is not ideal for large downloads, which can result from collecting dumps. If collecting large dumps, its recommended to use `curl` to make those requests directly. The swagger UI will provide the curl command to make it easy to copy/paste into a terminal window.

## OpenAPI documentation

The OpenAPI definition for the dotnet-monitor API can be found at [openapi.json](openapi.json). This can be used to generate a client stub for calling the API from your own tools. 