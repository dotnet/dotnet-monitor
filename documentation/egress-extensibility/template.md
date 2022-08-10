# Templates For Egress Extensibility

All `Verified` egress extensions that rely on user configuration being passed through `dotnet monitor` are required to include a JSON template to simplify the set-up process for users. The template should be named "name-of-egress-extension.json" and stored in the `Templates` directory (**Include link**).

## How-To Design a Template

Templates are designed to make it easy for users to interact with extensions without needing an in-depth understanding of the egress provider. A template includes the required/optional properties used in configuration, a brief description of each property (and, when necessary, how to find it), and a schema to aid in code autocompletion. Users should be able to configure the extension without needing to reference online documentation, only needing to paste in the necessary values to get their scenario up-and-running.

## Template Example

```json
{
  "$schema": "https://raw.githubusercontent.com/dotnet/dotnet-monitor/main/documentation/schema.json", /*Your schema link goes here*/
  "Egress": {
    "AzureEgressProviders": {
      "PROVIDER_NAME_GOES_HERE": {
        "AccountKey": "", /*REQUIRED - The account key used to access the Azure blob storage account; must be specified if `accountKeyName` is not specified.*/
	      "AccountUri": "", /*The URI of the Azure blob storage account.*/
	      "ContainerName": "", /*REQUIRED - The name of the container to which the blob will be egressed. If egressing to the root container, use the "$root" sentinel value.*/
	      "BlobPrefix": "", /*OPTIONAL - Optional path prefix for the artifacts to egress.*/
        ... Add the rest here
	    }
	  }
  }
}
```
