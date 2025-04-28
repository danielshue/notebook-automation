#!/bin/bash
# Wrapper script for generate_video_meta_from_onedrive.py to simplify common usage patterns

# Default options
DEBUG_FLAG=""
SINGLE_FILE=""
FOLDER=""
RETRY_FAILED=""
FORCE_FLAG=""
NO_SUMMARY=""
TIMEOUT="15"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    -f|--file)
      SINGLE_FILE="$2"
      shift 2
      ;;
    --folder)
      FOLDER="$2"
      shift 2
      ;;
    -d|--debug)
      DEBUG_FLAG="--debug"
      shift
      ;;
    -r|--retry)
      RETRY_FAILED="--retry-failed"
      shift
      ;;
    --force)
      FORCE_FLAG="--force"
      shift
      ;;
    --no-summary)
      NO_SUMMARY="--no-summary"
      shift
      ;;
    -t|--timeout)
      TIMEOUT="$2"
      shift 2
      ;;
    *)
      echo "Unknown option: $1"
      exit 1
      ;;
  esac
done

# Run the script with the appropriate options
if [ -n "$SINGLE_FILE" ]; then
  # Process a single file
  python generate_video_meta_from_onedrive.py -f "$SINGLE_FILE" $DEBUG_FLAG $FORCE_FLAG $NO_SUMMARY --timeout $TIMEOUT
elif [ -n "$FOLDER" ]; then
  # Process a folder
  python generate_video_meta_from_onedrive.py --folder "$FOLDER" $DEBUG_FLAG $FORCE_FLAG $NO_SUMMARY --timeout $TIMEOUT
elif [ -n "$RETRY_FAILED" ]; then
  # Retry failed files
  python generate_video_meta_from_onedrive.py $RETRY_FAILED $DEBUG_FLAG $FORCE_FLAG $NO_SUMMARY --timeout $TIMEOUT
else
  # Process all files
  python generate_video_meta_from_onedrive.py $DEBUG_FLAG $FORCE_FLAG $NO_SUMMARY --timeout $TIMEOUT
fi
