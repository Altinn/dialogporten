#!/bin/bash

API_VERSION=${API_VERSION:-v1}
API_ENVIRONMENT=${API_ENVIRONMENT:-yt01}
NUMBER_OF_ENDUSERS=${NUMBER_OF_ENDUSERS:-2799}
failed=0
kubectl config set-context --current --namespace=dialogporten

help() {
    echo "Usage: $0 [OPTIONS]"
    echo "Options:"
    echo "  -f, --filename       Specify the filename of the k6 script archive"
    echo "  -c, --configmapname  Specify the name of the configmap to create"
    echo "  -n, --name           Specify the name of the test run"
    echo "  -v, --vus            Specify the number of virtual users"
    echo "  -d, --duration       Specify the duration of the test"
    echo "  -p, --parallelism    Specify the level of parallelism"
    echo "  -b, --breakpoint     Flag to set breakpoint test or not"
    echo "  -a, --abort          Flag to specify whether to abort on fail or not, only used in breakpoint tests"
    echo "  -h, --help           Show this help message"
    exit 0
}

print_logs() {
    POD_LABEL="k6-test=$name"
    K8S_CONTEXT="${K8S_CONTEXT:-k6tests-cluster}"
    K8S_NAMESPACE="${K8S_NAMESPACE:-default}"
    LOG_TIMEOUT="${LOG_TIMEOUT:-60}"
    
    # Verify kubectl access
    if ! kubectl get pods &>/dev/null; then
        echo "Error: Failed to access Kubernetes cluster"
        return 1
    fi
    for pod in $(kubectl get pods -l "$POD_LABEL" -o name); do 
        if [[ $pod != *"initializer"* ]]; then
            echo ---------------------------
            echo $pod
            echo ---------------------------
            kubectl logs --tail=-1 $pod
            status=`kubectl get $pod -o jsonpath='{.status.phase}'`
            if [ "$status" != "Succeeded" ]; then
                failed=1
            fi
            echo
        fi
    done
}

breakpoint=false
abort_on_fail=false

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
        -b|--breakpoint)    
            breakpoint="$2"
            shift 2
            ;;
        -a|--abort)
            abort_on_fail="$2"
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
name=$(echo "$name" | tr '[:upper:]' '[:lower:]')
configmapname=$(echo "$configmapname" | tr '[:upper:]' '[:lower:]')
# Set testid to name + timestamp
testid="${name}_$(date '+%Y%m%dT%H%M%S')"

archive_args=""
if $breakpoint; then
    archive_args="-e stages_target=$vus -e stages_duration=$duration -e abort_on_fail=$abort_on_fail -e BREAKPOINT=true"
fi
# Create the k6 archive

if ! k6 archive $filename \
     -e API_VERSION="$API_VERSION" \
     -e API_ENVIRONMENT="$API_ENVIRONMENT" \
     -e NUMBER_OF_ENDUSERS="$NUMBER_OF_ENDUSERS" \
     -e TESTID=$testid $archive_args; then
    echo "Error: Failed to create k6 archive"
    exit 1
fi
   
# Create the configmap from the archive
if ! kubectl get configmap $configmapname &>/dev/null; then
  if ! kubectl create configmap $configmapname --from-file=archive.tar; then
      echo "Error: Failed to create configmap"
      rm archive.tar
      exit 1
  fi
fi


# Create the config.yml file from a string
arguments="--out experimental-prometheus-rw --vus=$vus --duration=$duration --tag testid=$testid --log-output=none"
if $breakpoint; then
    arguments="--out experimental-prometheus-rw --tag testid=$testid"
fi
cat <<EOF > config.yml
apiVersion: k6.io/v1alpha1
kind: TestRun
metadata:
  name: $name
  namespace: dialogporten
spec:
  arguments: $arguments 
  parallelism: $parallelism
  script:
    configMap:
      name: $configmapname
      file: archive.tar
  runner:
    env:
      - name: K6_PROMETHEUS_RW_SERVER_URL
        value: "http://kube-prometheus-stack-prometheus.monitoring:9090/api/v1/write"
      - name: K6_PROMETHEUS_RW_TREND_STATS
        value: "avg,min,med,max,p(95),p(99),p(99.5),p(99.9),count"
    envFrom:
    - secretRef:
        name: "token-generator-creds"
    metadata:
      labels:
        k6-test: $name
    resources:
      requests:
        cpu: 250m
        memory: 200Mi
    
EOF
# Apply the config.yml configuration
kubectl apply -f config.yml

# Wait for the job to finish
wait_timeout="${duration}200s"
kubectl wait --for=jsonpath='{.status.stage}'=finished testrun/$name --timeout=$wait_timeout
# Print the logs of the pods
print_logs

cleanup() {
    local exit_code=$failed
    echo "Sleeping for 15s and then cleaning up resources..."
    sleep 15
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