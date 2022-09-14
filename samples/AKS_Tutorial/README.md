### Dotnet Monitor AKS Tutorial
[![Video Tutorial For Using Dotnet Monitor As A Sidecar In AKS](https://i.ytimg.com/vi/3nzZO34nUFQ/hq720.jpg)](https://www.youtube.com/watch?v=3nzZO34nUFQ)

#### Commands Used In The Video

* 2:06 - `az acr login --name <name-of-acr>`
* 2:09 - `az aks create --resource-group <rg-name> --name <cluster-name> --node-count <number-of-nodes> --generate-ssh-keys --attach-acr <acr-name>`
* 2:14 - `az aks get-credentials --resource-group <rg-name> --name <cluster-name>`
* 2:17 - `kubectl config get-contexts`
* 2:21 - `kubectl get nodes`
* 5:38 - `kubectl apply -f egressmap.yaml`
* 5:48 - `kubectl create configmap dotnet-monitor-triggers --from-file=settings.json`
* 6:08 - `dotnet monitor generatekey`
* 6:18 - `kubectl create secret generic apikey --from-literal=Authentication__MonitorApiKey__Subject='...' --from-literal=Authentication__MonitorApiKey__PublicKey='...'`
* 6:27 - `kubectl get configmaps`
* 6:27 - `kubectl get secrets`
* 6:32 - `kubectl apply -f deploy.yaml`
* 6:41 - `kubectl get pods`
* 6:46 - `kubectl logs <pod-name> monitor`
* 7:14 - `kubectl get pods`
* 7:20 - `kubectl port-forward <pod-name> 80`
* 7:20 - `kubectl port-forward <pod-name> 52323`
* 7:42 - `curl -v -H "Authorization: Bearer <bearer-token>" http://localhost:52323/processes`
* 8:02 - `curl -v -H "Authorization: Bearer <bearer-token>" http://localhost:52323/gcdump?egressProvider=monitorBlob`
* 8:18 - `curl -v -H "Authorization: Bearer <bearer-token>" http://localhost:52323/gcdump?egressProvider=monitorFile`
* 8:24 - `kubectl exec -it <pod-name> -c monitor --/bin/sh`
* 8:58 - `curl -v http://localhost:80/Invalid`
