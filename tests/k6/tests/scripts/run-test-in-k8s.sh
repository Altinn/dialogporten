#!/bin/bash

tokengenuser=${TOKEN_GENERATOR_USERNAME}
tokengenpasswd=${TOKEN_GENERATOR_PASSWORD}

# Validate required environment variables
if [ -z "$TOKEN_GENERATOR_USERNAME" ] || [ -z "$TOKEN_GENERATOR_PASSWORD" ]; then
    echo "Error: TOKEN_GENERATOR_USERNAME and TOKEN_GENERATOR_PASSWORD must be set"
    exit 1
fi

help() {
    echo "Usage: $0 [OPTIONS]"
    echo "Options:"
    echo "  -f, --filename       Specify the filename of the k6 script archive"
    echo "  -c, --configmapname  Specify the name of the configmap to create"
    echo "  -n, --name           Specify the name of the test run"
    echo "  -v, --vus            Specify the number of virtual users"
    echo "  -d, --duration       Specify the duration of the test"
    echo "  -p, --parallelism    Specify the level of parallelism"
    echo "  -h, --help           Show this help message"
    exit 0
}

print_logs() {
    POD_LABEL="k6-test=$name"
    K8S_CONTEXT="${K8S_CONTEXT:-k6tests-cluster}"
    K8S_NAMESPACE="${K8S_NAMESPACE:-default}"
    LOG_TIMEOUT="${LOG_TIMEOUT:-60}"
    # Verify kubectl access
    if ! kubectl --context "$K8S_CONTEXT" -n "$K8S_NAMESPACE" get pods &>/dev/null; then
        echo "Error: Failed to access Kubernetes cluster"
        return 1
    fi
    for pod in $(kubectl --context "$K8S_CONTEXT" -n "$K8S_NAMESPACE" get pods -l "$POD_LABEL" -o name); do 
        if [[ $pod != *"initializer"* ]]; then
            echo ---------------------------
            echo $pod
            echo ---------------------------
            kubectl --context "$K8S_CONTEXT" -n "$K8S_NAMESPACE" logs --tail=-1 $pod
        fi
    done
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        -h|--help)
            help
            ;;
        -f|--filename)
            filename="$2"
            shift 2
            ;;
        -c|--configmapname)
            configmapname="$2"
            shift 2
            ;;
        -n|--name)
            name="$2"
            shift 2
            ;;
        -v|--vus)
            vus="$2"
            shift 2
            ;;
        -d|--duration)
            duration="$2"
            shift 2
            ;;
        -p|--parallelism)
            parallelism="$2"
            shift 2
            ;;
        *)
            echo "Invalid option: $1"
            help
            exit 1
            ;;
    esac
done

# Validate required arguments
missing_args=()
[ -z "$filename" ] && missing_args+=("filename (-f)")
[ -z "$configmapname" ] && missing_args+=("configmapname (-c)")
[ -z "$name" ] && missing_args+=("name (-n)")
[ -z "$vus" ] && missing_args+=("vus (-v)")
[ -z "$duration" ] && missing_args+=("duration (-d)")
[ -z "$parallelism" ] && missing_args+=("parallelism (-p)")

if [ ${#missing_args[@]} -ne 0 ]; then
    echo "Error: Missing required arguments: ${missing_args[*]}"
    help
    exit 1
fi

k6 archive $filename -e API_VERSION=v1 -e API_ENVIRONMENT=yt01 -e TOKEN_GENERATOR_USERNAME=$tokengenuser -e TOKEN_GENERATOR_PASSWORD=$tokengenpasswd
# Create configmap from archive.tar
kubectl create configmap $configmapname --from-file=archive.tar

# Create the config.yml file from a string
cat <<EOF > config.yml
apiVersion: k6.io/v1alpha1
kind: TestRun
metadata:
  name: $name
spec:
  arguments: --out experimental-prometheus-rw --vus=$vus --duration=$duration
  parallelism: $parallelism
  script:
    configMap:
      name: $configmapname
      file: archive.tar
  runner:
    env:
      - name: K6_PROMETHEUS_RW_SERVER_URL
        value: "http://kube-prometheus-stack-prometheus.monitoring:9090/api/v1/write"
    metadata:
      labels:
        k6-test: $name
EOF
# Apply the config.yml configuration
kubectl apply -f config.yml

# Wait for the job to finish
wait_timeout="${duration}100s"
kubectl --context k6tests-cluster wait --for=jsonpath='{.status.stage}'=finished testrun/$name --timeout=$wait_timeout

# Print the logs of the pods
print_logs

cleanup() {
    local exit_code=$?
    echo "Cleaning up resources..."
    
    if [ -f "config.yml" ]; then
        kubectl delete -f config.yml --ignore-not-found || true
        rm -f config.yml
    fi
    
    if kubectl get configmap $configmapname &>/dev/null; then
        kubectl delete configmap $configmapname --ignore-not-found || true
    fi
    
    rm -f archive.tar
    
    exit $exit_code
}
trap cleanup EXIT