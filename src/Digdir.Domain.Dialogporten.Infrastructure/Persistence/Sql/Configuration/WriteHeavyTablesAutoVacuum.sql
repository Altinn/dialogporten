-- Override autovacuum settings for write heavy tables

DO $$
DECLARE

  vac_scale     CONSTANT numeric := 0.005;  -- 0.5%
  vac_threshold CONSTANT integer := 1000;   -- min changed rows before vacuum
  ana_scale     CONSTANT numeric := 0.002;  -- 0.2%
  ana_threshold CONSTANT integer := 500;    -- min changed rows before analyze
  cost_limit    CONSTANT integer := 8000;   -- allow deeper cleanup per run
  cost_delay    CONSTANT integer := 2;      -- ms

  fq_tables CONSTANT text[] := ARRAY[
    'public.Actor',
    'public.Attachment',
    'public.AttachmentUrl',
    'public.Dialog',
    'public.DialogActivity',
    'public.DialogApiAction',
    'public.DialogApiActionEndpoint',
    'public.DialogContent',
    'public.DialogEndUserContext',
    'public.DialogEndUserContextSystemLabel',
    'public.DialogGuiAction',
    'public.DialogSearchTag',
    'public.DialogSeenLog',
    'public.DialogServiceOwnerContext',
    'public.DialogServiceOwnerLabel',
    'public.DialogTransmission',
    'public.DialogTransmissionContent',
    'public.LabelAssignmentLog',
    'public.Localization',
    'public.LocalizationSet',
    'search.DialogSearch',
    'search.DialogSearchRebuildQueue'
  ];

  fq    text;
  sch   text;
  rel   text;
sql   text;
BEGIN
  FOREACH fq IN ARRAY fq_tables LOOP
    sch := split_part(fq, '.', 1);
    rel := split_part(fq, '.', 2);

  sql := format($f$
      ALTER TABLE %I.%I SET (
        autovacuum_enabled = true,
        autovacuum_vacuum_scale_factor   = %s,
        autovacuum_vacuum_threshold      = %s,
        autovacuum_analyze_scale_factor  = %s,
        autovacuum_analyze_threshold     = %s,
        autovacuum_vacuum_cost_limit     = %s,
        autovacuum_vacuum_cost_delay     = %s
      )$f$, sch, rel,
      vac_scale, vac_threshold, ana_scale, ana_threshold, cost_limit, cost_delay);

  EXECUTE sql;
END LOOP;
END$$;
