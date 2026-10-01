# Evidencia estatica del codigo Laravel

Generado por scripts/Inventariar-Referencia.ps1. Lectura de codigo solamente; no prueba el esquema desplegado ni ejecuta pruebas. Los hashes identifican la version inspeccionada.

## routes

### routes/console.php

```text
13: Schedule::command('tdv2:sincronizaciones-procesar')->everyMinute();
```

### routes/web.php

```text
19: Route::get('/', [AuthController::class, 'login'])->name('home');
20: Route::get('/connect', [AuthController::class, 'connect'])->middleware('throttle:microsoft-login')->name('connect');
23: Route::post('/logout', [AuthController::class, 'logout'])->name('logout');
25: Route::delete('/actuar-como-usuario', [RepresentationController::class, 'destroy'])->name('representation.destroy');
26: Route::delete('/vista-prueba', [PreviewController::class, 'destroy'])->name('preview.destroy');
27: Route::get('/acceso-restringido', function (Request $request) {
36: Route::middleware(['MsGraphAuthenticated', ApplyRepresentation::class])->group(function () {
37: Route::get('/user/photo', [UserPhotoController::class, 'getPhoto'])->middleware('throttle:60,1')->name('user.photo');
38: Route::get('/user/modules', fn (Request $request, NexoAccessService $nexo) => response()->json($nexo->navigation($request->attributes->get('tdv2.representation')['profile'] ?? $request->attributes->get('nexo_access')))
41: Route::get('/configuracion', [ConfigurationController::class, 'index'])->middleware('configuration.access')->name('configuration.index');
42: Route::middleware('configuration.access:sincronizaciones')->group(function () {
43: Route::get('/configuracion/sincronizaciones', [SyncController::class, 'index'])->name('sync.index');
44: Route::put('/configuracion/sincronizaciones/programacion', [SyncController::class, 'update'])->middleware('throttle:20,1');
45: Route::post('/configuracion/sincronizaciones/ejecutar', [SyncController::class, 'store'])->middleware('throttle:10,1');
47: Route::middleware('configuration.access:pruebas_acceso')->group(function () {
48: Route::get('/configuracion/pruebas-acceso', [ConfigurationController::class, 'tools'])->name('access-tools.index');
49: Route::get('/configuracion/pruebas-acceso/actuar-como-usuario', [RepresentationController::class, 'index'])->name('representation.index');
50: Route::get('/actuar-como-usuario', fn () => to_route('representation.index'));
51: Route::get('/actuar-como-usuario/personas', [RepresentationController::class, 'search'])->middleware('throttle:30,1');
52: Route::post('/actuar-como-usuario', [RepresentationController::class, 'store'])->middleware('throttle:10,1');
53: Route::middleware('module.access:procesos_operativos')->group(function () {
54: Route::get('/configuracion/pruebas-acceso/rol-area', [PreviewController::class, 'index'])->name('preview.index');
55: Route::get('/vista-prueba', fn () => to_route('preview.index'));
56: Route::post('/vista-prueba', [PreviewController::class, 'store'])->middleware('throttle:20,1')->name('preview.store');
61: Route::middleware('module.access:procesos_operativos')->group(function () {
62: Route::middleware(ApplyPreview::class)->group(function () {
63: Route::get('/inicio', [FormController::class, 'index'])->name('inicio');
64: Route::get('/formatos/{ur}', [FormController::class, 'show'])->name('formatos.show');
65: Route::put('/formatos/{ur}', [FormController::class, 'update'])->middleware('throttle:120,1')->name('formatos.update');
66: Route::get('/colaboradores', [CollaboratorController::class, 'index'])->name('colaboradores.index');
67: Route::get('/colaboradores/personas', [CollaboratorController::class, 'search'])->middleware('throttle:60,1')->name('colaboradores.search');
68: Route::post('/colaboradores', [CollaboratorController::class, 'store'])->middleware('throttle:30,1')->name('colaboradores.store');
69: Route::delete('/colaboradores/{collaboration}', [CollaboratorController::class, 'destroy'])->middleware('throttle:30,1')->name('colaboradores.destroy');
```

## database/migrations

### database/migrations/0001_01_01_000000_create_users_table.php

```text
14: Schema::create('users', function (Blueprint $table) {
15: $table->id();
16: $table->string('name');
17: $table->string('email')->unique();
18: $table->timestamp('email_verified_at')->nullable();
19: $table->string('password');
20: $table->rememberToken();
21: $table->timestamps();
24: Schema::create('password_reset_tokens', function (Blueprint $table) {
25: $table->string('email')->primary();
26: $table->string('token');
27: $table->timestamp('created_at')->nullable();
30: Schema::create('sessions', function (Blueprint $table) {
31: $table->string('id')->primary();
32: $table->foreignId('user_id')->nullable()->index();
33: $table->string('ip_address', 45)->nullable();
34: $table->text('user_agent')->nullable();
35: $table->longText('payload');
36: $table->integer('last_activity')->index();
45: Schema::dropIfExists('users');
46: Schema::dropIfExists('password_reset_tokens');
47: Schema::dropIfExists('sessions');
```

### database/migrations/0001_01_01_000001_create_cache_table.php

```text
14: Schema::create('cache', function (Blueprint $table) {
15: $table->string('key')->primary();
16: $table->mediumText('value');
17: $table->integer('expiration')->index();
20: Schema::create('cache_locks', function (Blueprint $table) {
21: $table->string('key')->primary();
22: $table->string('owner');
23: $table->integer('expiration')->index();
32: Schema::dropIfExists('cache');
33: Schema::dropIfExists('cache_locks');
```

### database/migrations/0001_01_01_000002_create_jobs_table.php

```text
14: Schema::create('jobs', function (Blueprint $table) {
15: $table->id();
16: $table->string('queue')->index();
17: $table->longText('payload');
18: $table->unsignedTinyInteger('attempts');
19: $table->unsignedInteger('reserved_at')->nullable();
20: $table->unsignedInteger('available_at');
21: $table->unsignedInteger('created_at');
24: Schema::create('job_batches', function (Blueprint $table) {
25: $table->string('id')->primary();
26: $table->string('name');
27: $table->integer('total_jobs');
28: $table->integer('pending_jobs');
29: $table->integer('failed_jobs');
30: $table->longText('failed_job_ids');
31: $table->mediumText('options')->nullable();
32: $table->integer('cancelled_at')->nullable();
33: $table->integer('created_at');
34: $table->integer('finished_at')->nullable();
37: Schema::create('failed_jobs', function (Blueprint $table) {
38: $table->id();
39: $table->string('uuid')->unique();
40: $table->text('connection');
41: $table->text('queue');
42: $table->longText('payload');
43: $table->longText('exception');
44: $table->timestamp('failed_at')->useCurrent();
53: Schema::dropIfExists('jobs');
54: Schema::dropIfExists('job_batches');
55: Schema::dropIfExists('failed_jobs');
```

### database/migrations/2026_04_15_225913_create_ms_graph_tokens_table.php

```text
11: Schema::create('ms_graph_tokens', function (Blueprint $table) {
12: $table->increments('id');
13: $table->integer('user_id')->nullable();
14: $table->string('email')->nullable();
15: $table->text('access_token');
16: $table->text('refresh_token')->nullable();
17: $table->string('expires');
18: $table->timestamps();
24: Schema::dropIfExists('ms_graph_tokens');
```

### database/migrations/2026_04_15_231424_create_roles_table.php

```text
8: Schema::create('roles', function (Blueprint $table) {
9: $table->id();
10: $table->string('name');
11: $table->string('description')->nullable();
12: $table->timestamps();
16: Schema::dropIfExists('roles');
```

### database/migrations/2026_04_15_231501_create_modules_table.php

```text
8: Schema::create('modules', function (Blueprint $table) {
9: $table->id();
10: $table->string('name');
11: $table->string('description')->nullable();
12: $table->string('route')->nullable();
13: $table->unsignedBigInteger('parent_module_id')->nullable();
14: $table->string('icon')->nullable();
15: $table->timestamps();
16: $table->foreign('parent_module_id')->references('id')->on('modules')->nullOnDelete();
20: Schema::dropIfExists('modules');
```

### database/migrations/2026_04_15_231541_create_module_role_table.php

```text
8: Schema::create('module_role', function (Blueprint $table) {
9: $table->id();
10: $table->unsignedBigInteger('module_id');
11: $table->unsignedBigInteger('role_id');
12: $table->timestamps();
13: $table->foreign('module_id')->references('id')->on('modules')->cascadeOnDelete();
14: $table->foreign('role_id')->references('id')->on('roles')->cascadeOnDelete();
18: Schema::dropIfExists('module_role');
```

### database/migrations/2026_04_15_231655_create_catalogo_ur_table.php

```text
8: Schema::create('catalogo_ur', function (Blueprint $table) {
9: $table->id();
10: $table->string('ur')->nullable();
11: $table->string('ur2')->nullable();
12: $table->string('dependencia')->nullable();
13: $table->string('descripcion')->nullable();
14: $table->timestamps();
18: Schema::dropIfExists('catalogo_ur');
```

### database/migrations/2026_04_15_231732_create_catalogo_table.php

```text
8: Schema::create('catalogo', function (Blueprint $table) {
9: $table->id();
10: $table->string('UR_direccion')->nullable();
11: $table->string('UR_subdireccion')->nullable();
12: $table->string('dependencia')->nullable();
13: $table->string('descripcion_dependencia')->nullable();
14: $table->string('puesto')->nullable();
15: $table->string('responsable')->nullable();
16: $table->string('email')->nullable();
17: $table->unsignedBigInteger('role_id')->nullable();
18: $table->text('observaciones')->nullable();
19: $table->timestamps();
20: $table->foreign('role_id')->references('id')->on('roles')->nullOnDelete();
24: Schema::dropIfExists('catalogo');
```

### database/migrations/2026_04_15_231905_create_components_table.php

```text
8: Schema::create('components', function (Blueprint $table) {
9: $table->id();
10: $table->unsignedBigInteger('module_id')->nullable();
11: $table->unsignedBigInteger('role_id')->nullable();
12: $table->string('component_name')->nullable();
13: $table->timestamps();
14: $table->foreign('module_id')->references('id')->on('modules')->nullOnDelete();
15: $table->foreign('role_id')->references('id')->on('roles')->nullOnDelete();
19: Schema::dropIfExists('components');
```

### database/migrations/2026_04_15_232043_create_activity_logs_table.php

```text
8: Schema::create('activity_logs', function (Blueprint $table) {
9: $table->id();
10: $table->string('user_email')->nullable();
11: $table->string('user_name')->nullable();
12: $table->unsignedBigInteger('role_id')->nullable();
13: $table->string('ur')->nullable();
14: $table->string('ur2')->nullable();
15: $table->string('entity')->nullable();
16: $table->unsignedBigInteger('id_registro')->nullable();
17: $table->string('action')->nullable();
18: $table->json('meta')->nullable();
19: $table->string('ip')->nullable();
20: $table->string('user_agent')->nullable();
21: $table->timestamps();
25: Schema::dropIfExists('activity_logs');
```

### database/migrations/2026_04_15_232109_create_codigospostales_table.php

```text
8: Schema::create('codigospostales', function (Blueprint $table) {
9: $table->id();
10: $table->string('cp')->nullable();
11: $table->string('estado')->nullable();
12: $table->string('municipio')->nullable();
13: $table->string('ciudad')->nullable();
14: $table->string('tipo_asentamiento')->nullable();
15: $table->string('asentamiento')->nullable();
16: $table->string('clave_oficina')->nullable();
20: Schema::dropIfExists('codigospostales');
```

### database/migrations/2026_04_15_232136_create_pide_estructura_table.php

```text
8: Schema::create('pide_estructura', function (Blueprint $table) {
9: $table->string('num_indicador')->primary();
10: $table->string('indicador')->nullable();
11: $table->string('lineabase')->nullable();
12: $table->string('meta_2027')->nullable();
13: $table->string('meta_2030')->nullable();
14: $table->string('metrica')->nullable();
15: $table->text('formula')->nullable();
16: $table->string('valor_1')->nullable();
17: $table->string('valor_2')->nullable();
18: $table->string('valor_3')->nullable();
19: $table->string('eje')->nullable();
20: $table->string('pide_dgpdi_2024')->nullable();
21: $table->string('pide_dgpdi_2025')->nullable();
22: $table->string('indicadores_institucionales_dgti')->nullable();
23: $table->string('diferencia_dgti_dgpi')->nullable();
24: $table->text('areas_involucradas')->nullable();
25: $table->text('documentos_ilda')->nullable();
26: $table->text('documentos_ilda_1')->nullable();
27: $table->timestamps();
31: Schema::dropIfExists('pide_estructura');
```

### database/migrations/2026_09_24_000001_secure_microsoft_identity.php

```text
13: Schema::table('users', function (Blueprint $table): void {
14: $table->uuid('microsoft_tenant_id')->nullable();
15: $table->uuid('microsoft_id')->nullable();
16: $table->unique(['microsoft_tenant_id', 'microsoft_id'], 'users_microsoft_identity_unique');
54: Schema::table('ms_graph_tokens', fn (Blueprint $table) => $table->unique('user_id', 'ms_graph_tokens_user_unique'));
62: Schema::table('ms_graph_tokens', fn (Blueprint $table) => $table->dropUnique('ms_graph_tokens_user_unique'));
63: Schema::table('users', function (Blueprint $table): void {
64: $table->dropUnique('users_microsoft_identity_unique');
65: $table->dropColumn(['microsoft_tenant_id', 'microsoft_id']);
```

### database/migrations/2026_09_25_000001_create_tdv2_ur_forms.php

```text
11: Schema::create('unidades_responsables_poa', function (Blueprint $t): void {
12: $t->string('id_ur', 32)->primary();
13: $t->integer('ejercicio');
14: $t->string('cve_ur', 32);
15: $t->string('desc_ur', 500)->nullable();
16: $t->string('num_empleado', 32)->nullable()->index();
17: $t->string('encargado')->nullable();
18: $t->string('id_ur_pertenece', 32)->nullable()->index();
19: $t->string('tipo_ur', 32)->nullable();
20: $t->integer('nivel_ur')->nullable()->index();
21: $t->string('estatus_ur', 32)->nullable();
22: $t->boolean('presente')->default(true)->index();
23: $t->timestamp('sincronizado_en')->nullable();
25: Schema::create('directorio_institucional', function (Blueprint $t): void {
26: $t->id();
27: $t->string('email', 254)->index();
28: $t->string('num_empleado', 32)->index();
29: $t->string('nombre');
30: $t->string('id_ur', 32)->index();
31: $t->unique(['email', 'id_ur']);
33: Schema::create('sincronizaciones_institucionales', function (Blueprint $t): void {
34: $t->id();
35: $t->json('resumen');
36: $t->timestamp('completada_en');
38: Schema::create('formatos_ur', function (Blueprint $t): void {
39: $t->id();
40: $t->string('id_ur', 32)->unique();
41: $t->foreign('id_ur')->references('id_ur')->on('unidades_responsables_poa')->restrictOnDelete();
42: $t->json('contenido');
43: $t->unsignedInteger('version')->default(1);
44: $t->unsignedTinyInteger('porcentaje')->default(0);
45: $t->string('actualizado_por', 254);
46: $t->timestamps();
48: Schema::create('colaboraciones_ur', function (Blueprint $t): void {
49: $t->id();
50: $t->string('email', 254)->index();
51: $t->string('num_empleado', 32);
52: $t->string('nombre');
53: $t->string('id_ur_origen', 32);
54: $t->string('id_ur_alcance', 32)->index();
55: $t->string('tipo', 24);
56: $t->unsignedBigInteger('nexo_concesion_id');
57: $t->unsignedBigInteger('nexo_rol_id');
58: $t->string('otorgado_por', 254);
59: $t->string('ur_otorgante', 32);
60: $t->timestamp('revocada_en')->nullable();
61: $t->boolean('retiro_central_pendiente')->default(false);
62: $t->timestamps();
63: $t->unique(['nexo_concesion_id', 'id_ur_alcance'], 'colaboracion_concesion_alcance_unique');
69: Schema::dropIfExists('colaboraciones_ur');
70: Schema::dropIfExists('formatos_ur');
71: Schema::dropIfExists('sincronizaciones_institucionales');
72: Schema::dropIfExists('directorio_institucional');
73: Schema::dropIfExists('unidades_responsables_poa');
```

### database/migrations/2026_09_30_000001_create_sync_management.php

```text
12: Schema::create('sincronizacion_catalogos', function (Blueprint $t): void {
13: $t->string('fuente', 12)->primary();
14: $t->unsignedInteger('registros');
15: $t->timestamp('completada_en');
17: Schema::create('ilda_informacion_area', function (Blueprint $t): void {
18: $t->string('id_origen', 40)->primary();
19: $t->string('ur2', 255)->nullable()->index();
20: $t->text('informacion_generada')->nullable();
21: $t->json('datos'); // Todas las columnas de cada fila, con sus valores originales.
22: $t->boolean('presente')->default(true)->index();
23: $t->timestamp('sincronizado_en');
25: Schema::create('sincronizacion_ejecuciones', function (Blueprint $t): void {
26: $t->uuid('id')->primary();
27: $t->string('fuentes', 12);
28: $t->string('origen', 20);
29: $t->string('solicitado_por', 254);
30: $t->string('estado', 20)->index();
31: $t->string('etapa', 12)->nullable();
32: $t->json('resultado');
33: $t->timestamp('solicitada_en');
34: $t->timestamp('iniciada_en')->nullable();
35: $t->timestamp('terminada_en')->nullable();
37: Schema::create('sincronizacion_configuracion', function (Blueprint $t): void {
38: $t->unsignedTinyInteger('id')->primary();
39: $t->boolean('activa')->default(false);
40: $t->unsignedSmallInteger('intervalo_minutos')->default(60);
41: $t->string('hora', 5)->default('08:00');
42: $t->string('zona_horaria', 64)->default('America/Ciudad_Juarez');
43: $t->boolean('incluir_ilda')->default(false);
44: $t->unsignedInteger('version')->default(1);
45: $t->timestamp('proxima_en')->nullable();
46: $t->timestamp('procesador_visto_en')->nullable();
47: $t->uuid('ejecucion_activa')->nullable();
48: $t->uuid('propietario')->nullable();
49: $t->timestamp('reserva_hasta')->nullable();
50: $t->string('actualizado_por', 254)->nullable();
51: $t->timestamp('updated_at')->nullable();
64: Schema::dropIfExists('sincronizacion_configuracion');
65: Schema::dropIfExists('sincronizacion_ejecuciones');
66: Schema::dropIfExists('ilda_informacion_area');
67: Schema::dropIfExists('sincronizacion_catalogos');
```

## tests

### tests/Feature/AdministratorAccessTest.php

```text
45: public function test_any_origin_level_edits_its_level_two_branch_and_reads_other_branches(string $origin): void
65: public function test_can_read_partial_answers_already_saved_by_other_areas(): void
78: public function test_affiliation_change_moves_edit_scope_without_losing_institutional_read(): void
89: public function test_ambiguous_nexo_affiliation_does_not_invent_a_branch_from_local_history_or_client_input(): void
100: public function test_missing_or_inactive_level_two_ancestor_never_grants_global_edit(): void
111: public function test_administrator_role_revocation_removes_branch_editing_on_next_request(): void
119: public function test_other_roles_do_not_bypass_the_explicit_admin_branch_limit(): void
130: public function test_admin_role_does_not_grant_delegation_by_itself(): void
136: public function test_administrator_receives_only_active_level_two_and_three_units_without_employee_data(): void
151: public function test_directory_is_removed_when_administrator_role_is_revoked(): void
```

### tests/Feature/CollaboratorTest.php

```text
28: public function test_local_assignment_uses_level_three_form_and_central_employee_origin(): void
42: public function test_administrator_can_search_assign_and_remove_with_their_own_authorized_identity(): void
69: public function test_administrator_without_central_delegation_sees_requirements_and_cannot_call_procedures(): void
83: public function test_administrator_affiliation_does_not_impersonate_the_institutional_manager(): void
101: public function test_administrator_combined_roles_do_not_delegate_outside_their_affiliation_branch(): void
115: public function test_administrator_with_ambiguous_affiliation_gets_a_clear_configuration_notice(): void
125: public function test_revoked_administrator_delegation_is_rechecked_before_the_next_assignment(): void
137: public function test_global_assignment_is_scoped_to_authorizing_level_two_unit(): void
149: public function test_forged_recipient_actor_unit_and_institutional_role_are_rejected_before_writing_nexo(): void
160: public function test_missing_nexo_delegation_cannot_be_replaced_with_local_responsibility(): void
167: public function test_failed_central_grant_creates_no_local_edit_access(): void
176: public function test_revocation_blocks_local_link_even_when_central_function_fails(): void
188: public function test_search_labels_subordinate_employee_with_shared_level_three_form(): void
198: public function test_nexo_rejects_a_forged_employee_origin_even_when_unit_is_inside_branch(): void
208: public function test_missing_or_ambiguous_published_identity_creates_no_local_access_after_central_grant(): void
```

### tests/Feature/ConfigurationSyncTest.php

```text
56: public function test_admin_navigation_and_module_routes_match_nexo_hierarchy(): void
63: public function test_roles_module_grants_parent_and_path_are_all_enforced(): void
81: public function test_preview_blocks_configuration_reads_writes_and_other_trials_until_exit(): void
93: public function test_settings_validate_and_use_revision_with_audited_changes(): void
106: public function test_manual_request_is_queued_not_downloaded_in_the_web_request_and_duplicates_blocked(): void
115: public function test_each_source_has_atomic_publication_history_and_real_actor_audit(): void
132: public function test_ilda_only_does_not_read_sii_and_repeat_is_idempotent(): void
144: public function test_partial_failure_is_visible_and_does_not_leak_connection_secrets(): void
157: public function test_reduction_or_duplicate_rejects_entire_ilda_snapshot(): void
172: public function test_source_removals_leave_tombstones_without_deleting_form_answers(): void
187: public function test_sql_failure_rolls_back_the_snapshot_and_run_can_be_retried(): void
201: public function test_audit_failure_rolls_back_catalog_publication(): void
220: public function test_scheduler_always_includes_sii_and_ilda_is_opt_in(): void
234: public function test_paused_automation_still_processes_manual_requests(): void
244: public function test_schedule_uses_ciudad_juarez_daily_time_and_coalesces_missed_intervals(): void
256: public function test_running_reservation_prevents_second_executor_and_new_requests(): void
274: public function test_expired_executor_cannot_publish_over_a_new_execution_or_release_its_lock(): void
293: public function test_ilda_connection_disabled_is_explicit_without_disabling_sii(): void
```

### tests/Feature/ExampleTest.php

```text
12: public function test_returns_a_successful_response()
```

### tests/Feature/GraphTokenTest.php

```text
25: public function test_refresh_rotates_encrypted_tokens_and_does_not_repeat_exchange(): void
38: public function test_refresh_without_rotation_keeps_previous_refresh_token(): void
47: public function test_photo_failure_does_not_disclose_tokens_or_end_session(): void
58: public function test_upgrade_encrypts_legacy_values_and_keeps_one_token_per_user(): void
```

### tests/Feature/IldaInventoryTest.php

```text
49: public function test_reads_exact_authorized_local_code_without_contacting_mysql(): void
61: public function test_preserves_saved_answers_and_only_introduces_new_ids_after_sync(): void
80: public function test_remote_outage_or_disabled_sync_does_not_affect_existing_local_copy(): void
87: public function test_pending_initial_sync_does_not_connect_or_write(): void
96: public function test_full_replica_preserves_all_rows_columns_nulls_and_blanks_beyond_form_limit(): void
110: public function test_does_not_match_partial_codes_or_copy_parent_rows_into_child(): void
117: public function test_diagnostic_download_does_not_publish_or_write(): void
126: public function test_guest_signin_does_not_flash_the_yellow_prompt(): void
```

### tests/Feature/InstitutionalSyncTest.php

```text
25: public function test_copies_only_ur_without_overwriting_existing_form(): void
38: public function test_cycle_and_partial_download_leave_last_copy_untouched(): void
63: public function test_check_mode_does_not_publish_data(): void
```

### tests/Feature/MicrosoftAuthenticationTest.php

```text
53: public function test_oauth_start_uses_institutional_tenant_state_and_s256_pkce(): void
67: public function test_common_tenant_cannot_start_authentication(): void
73: public function test_invalid_state_and_expired_attempt_do_not_exchange_code(): void
85: public function test_provider_error_requires_matching_state_and_consumes_valid_attempt(): void
95: public function test_callback_requires_nexo_before_creating_local_user_or_tokens(): void
105: public function test_successful_callback_binds_identity_encrypts_tokens_and_preserves_public_home(): void
136: public function test_same_email_with_different_microsoft_object_cannot_take_over_local_identity(): void
148: public function test_external_email_does_not_authenticate(): void
```

### tests/Feature/NexoAccessTest.php

```text
29: public function test_public_home_preserves_session_and_does_not_require_nexo(): void
38: public function test_private_routes_deny_anonymous_and_unverified_sessions(): void
48: public function test_access_uses_guard_identity_and_all_effective_roles(): void
60: public function test_module_revocation_takes_effect_on_next_request(): void
70: public function test_membership_or_role_revocation_denies_every_private_route(): void
82: public function test_nexo_failure_does_not_fall_back_to_local_catalog(): void
92: public function test_wrong_app_level_key_and_route_fail_closed(): void
105: public function test_disabled_application_and_non_individual_accounts_are_denied(): void
115: public function test_logout_remains_available_during_outage_and_requires_post(): void
125: public function test_logout_rejects_missing_csrf_token_outside_testing_bypass(): void
133: public function test_configuration_command_checks_actual_module_for_person(): void
139: public function test_connection_resolves_the_id_without_environment_or_browser_input(): void
157: public function test_ambiguous_connection_never_selects_an_arbitrary_application(): void
171: public function test_invalid_published_id_cannot_authorize_or_choose_a_function(int|string $id): void
```

### tests/Feature/NexoIdentityTest.php

```text
25: public function test_responsibility_uses_employee_number_even_when_affiliation_points_elsewhere(): void
34: public function test_multiple_responsibilities_work_without_a_unique_affiliation(): void
45: public function test_affiliation_and_stale_local_directory_cannot_make_a_person_responsible(): void
54: public function test_employee_numbers_preserve_leading_zeroes_and_changes_apply_on_next_request(): void
65: public function test_collaboration_cannot_survive_employee_reassignment_or_ambiguous_affiliation(): void
76: public function test_admin_without_employee_number_keeps_global_read_but_cannot_edit(): void
85: public function test_outdated_nexo_publication_is_reported_without_using_local_identity(): void
92: public function test_diagnostic_reports_published_employee_and_actual_responsibilities(): void
```

### tests/Feature/PreviewTest.php

```text
37: public function test_only_the_current_real_administrator_can_open_or_start_a_preview(): void
45: public function test_responsible_preview_uses_the_same_scope_and_never_changes_the_real_identity(): void
64: public function test_level_three_and_lower_local_collaborators_share_only_the_level_three_form(): void
74: public function test_dependency_collaborator_and_administrator_scopes_stay_in_the_selected_branch(): void
86: public function test_server_rejects_every_business_mutation_and_delegated_search_during_preview(): void
103: public function test_user_mode_is_removed_and_forged_requests_cannot_start_it(): void
111: public function test_revoking_administrator_does_not_leave_an_active_preview_or_allow_a_write(): void
122: public function test_expiration_requires_explicit_exit_and_does_not_restore_writes_automatically(): void
133: public function test_invalid_role_units_and_unknown_accounts_cannot_start_preview(): void
```

### tests/Feature/RepresentationTest.php

```text
57: public function test_start_preserves_microsoft_identity_and_never_exposes_the_token(): void
69: public function test_write_uses_effective_scope_and_atomically_records_both_identities(): void
85: public function test_delegated_search_grant_and_revoke_use_explicit_representation_transport(): void
105: public function test_stale_tab_write_is_rejected_before_any_central_call(): void
112: public function test_read_only_session_has_disabled_editing_and_rejects_a_forged_write(): void
122: public function test_revoked_representation_never_falls_back_to_admin_and_exit_remains_available(): void
134: public function test_target_module_revocation_blocks_routes_and_nested_contexts_are_rejected(): void
145: public function test_unavailable_nexo_blocks_writes(): void
```

### tests/Feature/UnitFormAccessTest.php

```text
30: public function test_level_three_has_one_shared_form_even_with_subordinate_units(): void
40: public function test_level_two_sees_own_and_subordinate_forms_but_only_edits_own(): void
50: public function test_subordinate_staff_requires_explicit_collaboration_and_live_central_grant(): void
63: public function test_assignment_does_not_survive_an_affiliation_change_or_missing_central_role(): void
74: public function test_global_collaborator_is_limited_to_the_granting_level_two_branch(): void
86: public function test_institutional_read_permission_adds_no_edit_permissions(): void
96: public function test_server_preserves_the_shared_version_and_rejects_overwrite(): void
115: public function test_fixed_questions_and_progress_cannot_be_removed_or_forged(): void
131: public function test_central_grant_outage_fails_closed_and_does_not_modify_answers(): void
140: public function test_saved_progress_reaches_one_hundred_only_after_all_counted_fields_are_filled(): void
```

### tests/frontend/area-directory.test.mjs

```text
34: test('directorio limitado a niveles 2 y 3 sin alterar el promedio de sus formatos', () => {
48: test('buscar una dependencia conserva sus ancestros y normaliza acentos', () => {
55: test('filtros conservan contexto pero no ofrecen el formato que no coincide', () => {
63: test('áreas huérfanas y ciclos quedan visibles una sola vez', () => {
73: test('cada área principal tiene su propio grupo incluso si el catálogo la anida', () => {
83: test('excluye otros niveles incluso con un catálogo antiguo o formatos inesperados', () => {
```

### tests/frontend/form-autosave.test.mjs

```text
41: test('convierte las evaluaciones vacías al objeto esperado por el servidor', () => {
46: test('serializa cambios durante el envío con la nueva versión', async () => {
77: test('un conflicto conserva borrador y bloquea nuevas escrituras', async () => {
94: test('consulta no modifica ni envía contenido incluso con guardado explícito', async () => {
106: test('error de validación admite corrección sin sobrescribir la versión', async () => {
128: test('normaliza nulos del contrato Laravel sin perder campos históricos', () => {
```

### tests/Unit/InstitutionalSourceTest.php

```text
16: public function test_source_reads_only_latest_ur_exercise_and_preserves_employee_identifier(): void
47: public function test_missing_responsibility_column_is_not_silently_accepted(): void
```

### tests/Unit/NexoDelegationContractTest.php

```text
15: public function test_shared_views_keep_application_specific_functions_and_bound_parameters(): void
```
