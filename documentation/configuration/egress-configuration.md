# Egress Configuration

When `dotnet-monitor` is used to produce artifacts such as dumps or traces, an egress provider enables the artifacts to be stored in a manner suitable for the hosting environment rather than streamed back directly.

## Azure blob storage egress provider

| Name | Type | Required | Description |
|---|---|---|---|
| accountUri | string | true | The URI of the Azure blob storage account.|
| containerName | string | true | The name of the container to which the blob will be egressed. If egressing to the root container, use the "$root" sentinel value.|
| blobPrefix | string | false | Optional path prefix for the artifacts to egress.|
| copyBufferSize | string | false | The buffer size to use when copying data from the original artifact to the blob stream.|
| accountKey | string | false | The account key used to access the Azure blob storage account; must be specified if `accountKeyName` is not specified.|
| sharedAccessSignature | string | false | The shared access signature (SAS) used to access the Azure blob and optionally queue storage accounts; if using SAS, must be specified if `sharedAccessSignatureName` is not specified.|
| accountKeyName | string | false | Name of the property in the Properties section that will contain the account key; must be specified if `accountKey` is not specified.|
| managedIdentityClientId | string | false | The ClientId of the ManagedIdentity that can be used to authorize egress. Note this identity must be used by the hosting environment (such as Kubernetes) and must also have a Storage role with appropriate permissions. |
| sharedAccessSignatureName | string | false | Name of the property in the Properties section that will contain the SAS token; if using SAS, must be specified if `sharedAccessSignature` is not specified.|
| queueName | string | false | The name of the queue to which a message will be dispatched upon writing to a blob.|
| queueAccountUri | string | false | The URI of the Azure queue storage account.|
| queueSharedAccessSignature | string | false | (6.3+) The shared access signature (SAS) used to access the Azure queue storage account; if using SAS, must be specified if `queueSharedAccessSignatureName` is not specified.|
| queueSharedAccessSignatureName | string | false | (6.3+) Name of the property in the Properties section that will contain the queue SAS token; if using SAS, must be specified if `queueSharedAccessSignature` is not specified.|
| metadata | Dictionary<string, string> | false | A mapping of metadata keys to environment variable names. The values of the environment variables will be added as metadata for egressed artifacts.|

> [!NOTE]
> Starting with `dotnet monitor` 7.0, all built-in metadata keys are prefixed with `DotnetMonitor_`; to avoid metadata naming conflicts, avoid prefixing your metadata keys with `DotnetMonitor_`.

### Example azureBlobStorage provider

<details>
  <summary>JSON</summary>

  ```json
  {
      "Egress": {
          "AzureBlobStorage": {
              "monitorBlob": {
                  "accountUri": "https://exampleaccount.blob.core.windows.net",
                  "containerName": "dotnet-monitor",
                  "blobPrefix": "artifacts",
                  "accountKeyName": "MonitorBlobAccountKey"
              }
          },
          "Properties": {
              "MonitorBlobAccountKey": "accountKey"
          }
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  Egress__AzureBlobStorage__monitorBlob__accountUri: "https://exampleaccount.blob.core.windows.net"
  Egress__AzureBlobStorage__monitorBlob__containerName: "dotnet-monitor"
  Egress__AzureBlobStorage__monitorBlob__blobPrefix: "artifacts"
  Egress__AzureBlobStorage__monitorBlob__accountKeyName: "MonitorBlobAccountKey"
  Egress__Properties__MonitorBlobAccountKey: "accountKey"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__accountUri
    value: "https://exampleaccount.blob.core.windows.net"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__containerName
    value: "dotnet-monitor"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__blobPrefix
    value: "artifacts"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__accountKeyName
    value: "MonitorBlobAccountKey"
  - name: DotnetMonitor_Egress__Properties__MonitorBlobAccountKey
    value: "accountKey"
  ```
</details>

### Example azureBlobStorage provider with queue

<details>
  <summary>JSON</summary>

  ```json
  {
      "Egress": {
          "AzureBlobStorage": {
              "monitorBlob": {
                  "accountUri": "https://exampleaccount.blob.core.windows.net",
                  "containerName": "dotnet-monitor",
                  "blobPrefix": "artifacts",
                  "accountKeyName": "MonitorBlobAccountKey",
                  "queueAccountUri": "https://exampleaccount.queue.core.windows.net",
                  "queueName": "dotnet-monitor-queue"
              }
          },
          "Properties": {
              "MonitorBlobAccountKey": "accountKey"
          }
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  Egress__AzureBlobStorage__monitorBlob__accountUri: "https://exampleaccount.blob.core.windows.net"
  Egress__AzureBlobStorage__monitorBlob__containerName: "dotnet-monitor"
  Egress__AzureBlobStorage__monitorBlob__blobPrefix: "artifacts"
  Egress__AzureBlobStorage__monitorBlob__accountKeyName: "MonitorBlobAccountKey"
  Egress__AzureBlobStorage__monitorBlob__queueAccountUri: "https://exampleaccount.queue.core.windows.net"
  Egress__AzureBlobStorage__monitorBlob__queueName: "dotnet-monitor-queue"
  Egress__Properties__MonitorBlobAccountKey: "accountKey"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__accountUri
    value: "https://exampleaccount.blob.core.windows.net"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__containerName
    value: "dotnet-monitor"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__blobPrefix
    value: "artifacts"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__accountKeyName
    value: "MonitorBlobAccountKey"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__queueAccountUri
    value: "https://exampleaccount.queue.core.windows.net"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__queueName
    value: "dotnet-monitor-queue"
  - name: DotnetMonitor_Egress__Properties__MonitorBlobAccountKey
    value: "accountKey"
  ```
</details>

#### azureBlobStorage Queue Message Format

The Queue Message's payload will be the blob name (`<BlobPrefix>/<ArtifactName>`; using the above example with an artifact named `mydump.dmp`, this would be `artifacts/mydump.dmp`) that is being egressed to blob storage. This is designed to be easily integrated into an Azure Function that triggers whenever a new message is added to the queue, providing you with the contents of the artifact as a stream. See [Azure Blob storage input binding for Azure Functions](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-blob-input?tabs=csharp#example) for an example.

## S3 storage egress provider

First Available: 8.0

| Name | Type | Required | Description |
|---|---|---|---|
| endpoint | string | false | An optional endpoint of S3 storage service. Can be left empty in case of using AWS. |
| bucketName | string | true | The name of the S3 Bucket to which the blob will be egressed. |
| accessKeyId | string | false | The AWS AccessKeyId for IAM user to login.  |
| secretAccessKey | string | false | The AWS SecretAccessKey associated AccessKeyId for IAM user to login. To login by access key id the 'secretAccessKey' must be set. |
| awsProfileName | string | false | The AWS profile name to be used for login. |
| awsProfilePath | string | false | The AWS profile path, if profile details not stored in default path. |
| regionName | string | false | A Region is a named set of AWS resources in the same geographical area. This option specifies the region to connect to. If the Endpoint is specified, this is the AuthenticationRegion; otherwise, it is the RegionEndpoint. |
| preSignedUrlExpiry | TimeStamp? | false | When specified, a pre-signed url is returned after successful upload; this value specifies the amount of time the generated pre-signed url should be accessible. The value has to be between 1 minute and 1 day. |
| forcePathStyle | bool | false | The boolean flag set for AWS connection configuration ForcePathStyle option. |
| copyBufferSize | int | false | The buffer size to use when copying data from the original artifact to the blob stream. There is a minimum size of 5 MB which is set when the given value is lower.|
| useKmsEncryption | bool | false | (9.0 Preview 6+) A boolean flag which controls whether the Egress should use KMS server side encryption. |
| kmsEncryptionKey | string | false | (9.0 Preview 6+) If UseKmsEncryption is true, this specifies the arn of the "customer managed" KMS encryption key to be used for server side encryption. If no value is set for this field then S3 will use an AWS managed key for KMS encryption. |

### Example S3 storage provider

<details>
  <summary>JSON with password</summary>

  ```json
  {
      "Egress": {
          "S3Storage": {
              "monitorS3Blob": {
                  "endpoint": "http://localhost:9000",
                  "bucketName": "myS3Bucket",
                  "accessKeyId": "minioUser",
                  "secretAccessKey": "mySecretPassword",
                  "regionName": "us-east-1",
                  "preSignedUrlExpiry" : "00:15:00",
                  "copyBufferSize": 1024
              }
          }
      }
  }
  ```
</details>

<details>
  <summary>JSON with customer managed KMS encryption</summary>

  ```json
  {
      "Egress": {
          "S3Storage": {
              "monitorS3Blob": {
                  "endpoint": "http://localhost:9000",
                  "bucketName": "myS3Bucket",
                  "useKmsEncryption": true,
                  "kmsEncryptionKey": "arn:aws:kms:{region}:{account-id}:key/{resource-id}"
              }
          }
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes Secret</summary>

  ```sh
  #!/bin/sh
  kubectl create secret generic my-s3-secrets \
  --from-literal=Egress__S3Storage__monitorS3Blob__bucketName=myS3Bucket \
  --from-literal=Egress__S3Storage__monitorS3Blob__accessKeyId=minioUser \
  --from-literal=Egress__S3Storage__monitorS3Blob__secretAccessKey=mySecretPassword \
  --from-literal=Egress__S3Storage__monitorS3Blob__regionName=us-east-1 \
  --dry-run=client -o yaml | kubectl apply -f -
 ```
</details>

### Authenticating to S3 using service accounts

First Available: 9.0 Preview 5

If running workloads in Kubernetes it is common to authenticate with AWS via Kubernetes service accounts ([AWS Documentation](https://docs.aws.amazon.com/eks/latest/userguide/pod-configuration.html)). This is supported in dotnet monitor if none of: `accessKeyId`, `secretAccessKey`, `awsProfileName` are specified. In this case dotnet monitor will fallback to load credentials to login using AWS default defined environment variables, this means that workloads running in EKS can utilize service accounts as discussed in the above AWS documentation.

Specifically the use of service accounts set the following environment variables which are detected by AWS SDK and used for authentication as a fallback:
 - AWS_REGION
 - AWS_ROLE_ARN
 - AWS_WEB_IDENTITY_TOKEN_FILE

## Filesystem egress provider

| Name | Type | Description |
|---|---|---|
| directoryPath | string | The directory path to which the stream data will be egressed.|
| intermediateDirectoryPath | string | The directory path to which the stream data will initially be written; if specified, the file will then be moved/renamed to the directory specified in 'directoryPath'.|

### Example fileSystem provider

<details>
  <summary>JSON</summary>

  ```json
  {
      "Egress": {
          "FileSystem": {
              "monitorFile": {
                  "directoryPath": "/artifacts",
                  "intermediateDirectoryPath": "/intermediateArtifacts"
              }
          }
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  Egress__FileSystem__monitorFile__directoryPath: "/artifacts"
  Egress__FileSystem__monitorFile__intermediateDirectoryPath: "/intermediateArtifacts"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_Egress__FileSystem__monitorFile__directoryPath
    value: "/artifacts"
  - name: DotnetMonitor_Egress__FileSystem__monitorFile__intermediateDirectoryPath
    value: "/intermediateArtifacts"
  ```
</details>
