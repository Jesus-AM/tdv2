-- SYNTHETIC Nexo publisher and functions. Not the Laravel implementation or institutional evidence.
CREATE TABLE fixture_units(id text PRIMARY KEY,parent text,employee text,level int);
CREATE TABLE fixture_delegation(email text,id_ur text,num_empleado text,nivel_ur int);
CREATE TABLE fixture_delegation_roles(clave text,rol_id bigint);
CREATE TABLE fixture_people(id bigint PRIMARY KEY,email text,nombre text,id_ur text,num_empleado text,desc_ur text);
CREATE TABLE fixture_capability(actor text PRIMARY KEY,permitido bool,escritura bool,alcance text,root text,duracion_minutos int);
CREATE TABLE fixture_representation(id uuid PRIMARY KEY,token text,actor_email text,email text,nombre text,id_ur text,
    escritura bool,expira_en timestamptz,motivo text,revoked bool DEFAULT false);
CREATE TABLE fixture_calls(id bigserial PRIMARY KEY,operation text,actor text,ur text,target text,transport text);
CREATE TABLE fixture_faults(operation text PRIMARY KEY,code text);
CREATE SEQUENCE fixture_grant_ids START 900;
CREATE VIEW nexo_delegacion AS SELECT * FROM fixture_delegation;
CREATE VIEW nexo_delegacion_roles AS SELECT * FROM fixture_delegation_roles;

CREATE FUNCTION fixture_within(area text,root text) RETURNS boolean LANGUAGE sql STABLE AS $$
 WITH RECURSIVE tree AS (SELECT id,parent,ARRAY[id] visited FROM fixture_units WHERE id=area
   UNION ALL SELECT u.id,u.parent,t.visited||u.id FROM fixture_units u JOIN tree t ON u.id=t.parent WHERE NOT u.id=ANY(t.visited))
 SELECT EXISTS(SELECT 1 FROM tree WHERE id=root)
$$;
CREATE FUNCTION fixture_fault(operation_name text) RETURNS void LANGUAGE plpgsql AS $$
DECLARE fault text;
BEGIN
 SELECT code INTO fault FROM fixture_faults WHERE operation=operation_name;
 IF fault IS NOT NULL THEN RAISE EXCEPTION USING ERRCODE=fault,MESSAGE='SYNTHETIC_NEXO_ERROR_DO_NOT_EXPOSE'; END IF;
END $$;
CREATE FUNCTION fixture_delegate(actor text,root text) RETURNS void LANGUAGE plpgsql AS $$
BEGIN
 IF NOT EXISTS(SELECT 1 FROM fixture_delegation d JOIN fixture_users u ON u.email=d.email
   JOIN fixture_units a ON a.id=d.id_ur WHERE d.email=actor AND d.id_ur=root AND d.nivel_ur=2
   AND u.num_empleado=d.num_empleado AND a.employee=d.num_empleado AND a.level=d.nivel_ur)
   OR NOT EXISTS(SELECT 1 FROM fixture_roles r WHERE r.email=actor AND rol_clave IN ('responsable_ur','administrador'))
 THEN RAISE EXCEPTION 'SYNTHETIC_NOT_DELEGATED'; END IF;
END $$;
CREATE FUNCTION nexo_a47_buscar_personas(actor text,ur text,q text,pagina integer)
 RETURNS TABLE(email text,nombre text,id_ur text,desc_ur text,total bigint)
 LANGUAGE plpgsql SECURITY DEFINER SET search_path=public,pg_temp AS $$
BEGIN
 PERFORM fixture_fault('buscar_personas'); PERFORM fixture_delegate(actor,ur);
 INSERT INTO fixture_calls(operation,actor,ur,transport) VALUES('buscar_personas',actor,ur,'direct');
 RETURN QUERY SELECT p.email,p.nombre,p.id_ur,p.desc_ur,count(*) OVER() FROM fixture_people p
   WHERE fixture_within(p.id_ur,ur) AND (p.email ILIKE '%'||q||'%' OR p.nombre ILIKE '%'||q||'%')
   ORDER BY p.id LIMIT 25 OFFSET (pagina-1)*25;
END $$;
CREATE FUNCTION nexo_a47_conceder_acceso(actor text,ur text,target_email text,role_id bigint,origin text)
 RETURNS TABLE(concesion_id bigint,creada boolean)
 LANGUAGE plpgsql SECURITY DEFINER SET search_path=public,pg_temp AS $$
DECLARE person fixture_people; grant_id bigint; role_key text;
BEGIN
 PERFORM fixture_fault('conceder_acceso'); PERFORM fixture_delegate(actor,ur);
 SELECT * INTO person FROM fixture_people p WHERE p.email=target_email AND p.id_ur=origin AND fixture_within(p.id_ur,ur);
 SELECT clave INTO role_key FROM fixture_delegation_roles WHERE rol_id=role_id;
 IF person.id IS NULL OR role_key IS NULL THEN RAISE EXCEPTION 'SYNTHETIC_INELIGIBLE'; END IF;
 SELECT g.concesion_id INTO grant_id FROM fixture_grants g WHERE g.email=target_email AND g.rol_id=role_id AND g.id_ur_acceso=origin AND g.origen='aplicacion';
 creada := grant_id IS NULL;
 IF grant_id IS NULL THEN
   grant_id := nextval('fixture_grant_ids'); INSERT INTO fixture_grants VALUES(grant_id,target_email,role_id,origin,'aplicacion');
 END IF;
 IF NOT EXISTS(SELECT 1 FROM fixture_faults WHERE operation='omit_identity') THEN
   IF NOT EXISTS(SELECT 1 FROM fixture_users WHERE fixture_users.email=target_email) THEN
     INSERT INTO fixture_users VALUES(person.id+100,person.id,target_email,person.nombre,'individual',person.num_empleado,origin,'adscripcion');
   END IF;
   INSERT INTO fixture_roles SELECT u.usuario_aplicacion_id,target_email,role_id,role_key,role_key FROM fixture_users u
     WHERE u.email=target_email AND NOT EXISTS(SELECT 1 FROM fixture_roles r WHERE r.email=target_email AND r.rol_id=role_id);
 END IF;
 INSERT INTO fixture_calls(operation,actor,ur,target,transport) VALUES('conceder_acceso',actor,ur,target_email,'direct');
 concesion_id := grant_id; RETURN NEXT;
END $$;
CREATE FUNCTION nexo_a47_retirar_acceso(actor text,ur text,grant_id bigint) RETURNS TABLE(retirada boolean)
 LANGUAGE plpgsql SECURITY DEFINER SET search_path=public,pg_temp AS $$
BEGIN
 PERFORM fixture_fault('retirar_acceso'); PERFORM fixture_delegate(actor,ur);
 IF EXISTS(SELECT 1 FROM fixture_grants g WHERE g.concesion_id=grant_id AND NOT fixture_within(g.id_ur_acceso,ur)) THEN RAISE EXCEPTION 'SYNTHETIC_FOREIGN_GRANT'; END IF;
 DELETE FROM fixture_grants WHERE concesion_id=grant_id;
 INSERT INTO fixture_calls(operation,actor,ur,transport) VALUES('retirar_acceso',actor,ur,'direct');
 retirada := true; RETURN NEXT;
END $$;
CREATE FUNCTION nexo_a47_representacion(input jsonb) RETURNS jsonb
 LANGUAGE plpgsql SECURITY DEFINER SET search_path=public,pg_temp AS $$
#variable_conflict use_variable
DECLARE action text := input->>'accion'; actor text := input->>'actor_email'; cap fixture_capability;
 selected fixture_representation; person fixture_users; response jsonb; wants_write bool;
BEGIN
 PERFORM fixture_fault('representacion');
 SELECT * INTO cap FROM fixture_capability WHERE fixture_capability.actor=actor;
 IF action='capacidad' THEN
   RETURN jsonb_build_object('permitido',coalesce(cap.permitido,false),'escritura',coalesce(cap.escritura,false),'alcance',cap.alcance,'duracion_minutos',cap.duracion_minutos);
 END IF;
 IF action='finalizar' THEN
   UPDATE fixture_representation SET revoked=true WHERE token=input->>'token' AND actor_email=actor;
   INSERT INTO fixture_calls(operation,actor,transport) VALUES(action,actor,'representation'); RETURN '{}'::jsonb;
 END IF;
 IF NOT coalesce(cap.permitido,false) THEN RAISE EXCEPTION 'SYNTHETIC_NO_CAPABILITY'; END IF;
 IF action='buscar' THEN
   SELECT coalesce(jsonb_agg(p),'[]'::jsonb) INTO response FROM (
     SELECT u.usuario_id,u.nombre,u.email,u."ID_UR" id_ur,u."ID_UR" desc_ur,
       (SELECT jsonb_agg(jsonb_build_object('clave',r.rol_clave,'nombre',r.rol_nombre)) FROM fixture_roles r WHERE r.email=u.email) roles
     FROM fixture_users u WHERE (cap.alcance='institucional' OR fixture_within(u."ID_UR",cap.root))
       AND (u.email ILIKE '%'||(input->>'q')||'%' OR u.nombre ILIKE '%'||(input->>'q')||'%')
     ORDER BY u.usuario_id LIMIT 26 OFFSET (coalesce((input->>'pagina')::integer,1)-1)*25
   ) p;
   INSERT INTO fixture_calls(operation,actor,transport) VALUES(action,actor,'representation');
   RETURN jsonb_build_object('personas',response);
 END IF;
 IF action='iniciar' THEN
   wants_write := (input->>'escritura')::boolean;
   SELECT * INTO person FROM fixture_users u WHERE u.email=input->>'email';
   IF person.email IS NULL OR (cap.alcance<>'institucional' AND NOT fixture_within(person."ID_UR",cap.root)) OR (wants_write AND NOT cap.escritura)
     THEN RAISE EXCEPTION 'SYNTHETIC_OUTSIDE_REPRESENTATION_SCOPE'; END IF;
   INSERT INTO fixture_representation VALUES(gen_random_uuid(),replace(gen_random_uuid()::text,'-','')||replace(gen_random_uuid()::text,'-',''),actor,person.email,
     person.nombre,person."ID_UR",wants_write,clock_timestamp()+make_interval(mins=>cap.duracion_minutos),input->>'motivo',false) RETURNING * INTO selected;
   INSERT INTO fixture_calls(operation,actor,target,transport) VALUES(action,actor,person.email,'representation');
   RETURN to_jsonb(selected);
 END IF;
 SELECT * INTO selected FROM fixture_representation WHERE token=input->>'token' AND actor_email=actor;
 wants_write := CASE WHEN action='validar' THEN coalesce((input->>'requiere_escritura')::boolean,false) ELSE action<>'buscar_personas' END;
 IF selected.id IS NULL OR selected.revoked OR selected.expira_en<=clock_timestamp() OR (wants_write AND (NOT selected.escritura OR NOT cap.escritura))
   OR NOT EXISTS(SELECT 1 FROM fixture_users WHERE email=selected.email)
   OR (cap.alcance<>'institucional' AND NOT fixture_within(selected.id_ur,cap.root)) THEN RAISE EXCEPTION 'SYNTHETIC_REPRESENTATION_REVOKED'; END IF;
 INSERT INTO fixture_calls(operation,actor,ur,target,transport) VALUES(action,actor,input->>'ur',selected.email,'representation');
 IF action='validar' THEN RETURN to_jsonb(selected)-'token'; END IF;
 IF action='buscar_personas' THEN
   SELECT coalesce(jsonb_agg(r),'[]'::jsonb) INTO response FROM nexo_a47_buscar_personas(selected.email,input->>'ur',input->>'q',(input->>'pagina')::integer) r;
 ELSIF action='conceder_acceso' THEN
   SELECT coalesce(jsonb_agg(r),'[]'::jsonb) INTO response FROM nexo_a47_conceder_acceso(selected.email,input->>'ur',input->>'email',(input->>'rol_id')::bigint,input->>'ur_origen') r;
 ELSIF action='retirar_acceso' THEN
   SELECT coalesce(jsonb_agg(r),'[]'::jsonb) INTO response FROM nexo_a47_retirar_acceso(selected.email,input->>'ur',(input->>'concesion_id')::bigint) r;
 ELSE RAISE EXCEPTION 'SYNTHETIC_UNKNOWN_ACTION'; END IF;
 RETURN response;
END $$;
REVOKE ALL ON FUNCTION nexo_a47_buscar_personas(text,text,text,integer),nexo_a47_conceder_acceso(text,text,text,bigint,text),nexo_a47_retirar_acceso(text,text,bigint),nexo_a47_representacion(jsonb) FROM PUBLIC;
