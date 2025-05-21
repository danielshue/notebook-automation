import traceback
try:
    from list_transcripts_with_videos import main
    main()
except Exception as e:
    traceback.print_exc()
