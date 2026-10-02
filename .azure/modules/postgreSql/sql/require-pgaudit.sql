-- Read-only prerequisite check, run against the application database before provisioning.
\set ON_ERROR_STOP on

DO $$
DECLARE
  preloaded text;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pgaudit') THEN
    RAISE EXCEPTION
      'pgaudit is not installed in database %. Run CREATE EXTENSION pgaudit there before provisioning.',
      current_database();
  END IF;

  -- shared_preload_libraries may only be examined by a superuser or a member of
  -- pg_read_all_settings, and the owner login is neither, so a denial here says nothing about the
  -- server and must not fail the run. The check above already rules out the case this would catch:
  -- pgaudit's own CREATE EXTENSION refuses unless the library is preloaded, so the extension
  -- cannot be present without it. This is a second look for servers where the setting is readable.
  BEGIN
    preloaded := current_setting('shared_preload_libraries');
  EXCEPTION WHEN insufficient_privilege THEN
    preloaded := NULL;
  END;

  IF preloaded IS NOT NULL AND position('pgaudit' in preloaded) = 0 THEN
    RAISE EXCEPTION
      'pgaudit is not in shared_preload_libraries (currently: %). Add it on the server before provisioning.',
      preloaded;
  END IF;
END $$;
