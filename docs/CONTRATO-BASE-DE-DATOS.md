# Contrato con la base de datos (para quien programa PostgreSQL)

El código usa listas en memoria. Para conectar PostgreSQL hay que escribir, en `src/Infraestructura`,
una clase por cada contrato (`I...Repositorio`) y cambiar su línea en `src/Api/Configuracion/RegistroDeServicios.cs`.
Los contratos viven en `src/Aplicacion/<Modulo>/`. Todos tienen los mismos 5 métodos:
`ObtenerAsync`, `ListarAsync`, `BuscarAsync(filtro)`, `AgregarAsync`, `ActualizarAsync`.

Las clases del negocio están en `src/Dominio`. Cada tabla lleva `id` (entero, llave primaria, autoincremental).

| Tabla | Columnas principales | Notas |
|---|---|---|
| usuario | correo (único), nombre_completo, rol, hash_contrasena (nulo), activo, intentos_fallidos, bloqueado_hasta | rol: Alumno, Coach, PersonalMedico, Directivo, Administrador |
| codigo_verificacion | correo, hash_codigo, creado, expira, intentos, verificado, hash_token_registro, usado | se puede limpiar lo vencido |
| actividad | nombre, tipo (Deportiva/Cultural), horario, coach_id → usuario, cupo_maximo, activa | |
| actividad_tipo_documento | actividad_id, tipo_documento_id | sale de `TiposDocumentoRequeridos` (lista de ids) |
| inscripcion | alumno_id, actividad_id, fecha, cancelada | único (alumno_id, actividad_id) cuando cancelada = falso |
| tipo_documento | nombre, descripcion, contexto (Actividad / NuevoIngresoMedico) | |
| documento | alumno_id, tipo_documento_id, contexto, referencia_id, nombre_original, archivo_almacenado, estado, motivo_rechazo, revisado_por_id, fecha_carga, fecha_revision | referencia_id = inscripcion.id o tramite_nuevo_ingreso.id según contexto |
| notificacion | usuario_id, mensaje, fecha, leida | |
| asistencia | actividad_id, alumno_id, fecha (date), presente, registrado_por_id | único (actividad_id, alumno_id, fecha) |
| tramite_nuevo_ingreso | alumno_id (único), fecha | |
| valoracion | alumno_id, medico_id, fecha, resultado, observaciones | |
| expediente_clinico | alumno_id (único), tipo_sangre, antecedentes_alergicos, antecedentes_cronicos, antecedentes_patologicos | dato sensible: cifrar columnas o disco (RNF3) |
| consulta | alumno_id, medico_id, fecha, motivo, diagnostico, es_lesion_deportiva, genera_ausencia | dato sensible |
| entrega_insumo | consulta_id, descripcion, cantidad | sale de `Consulta.Entregas` |
| atencion_primeros_auxilios | fecha, lugar, motivo, atencion_brindada, alumno_id (nulo), medico_id | |
| urgencia | fecha, alumno_id (nulo), medico_id, triage (Verde/Amarillo/Rojo), diagnostico_preliminar, derivacion (nula) | |
| campana | titulo, descripcion, fecha_inicio, fecha_fin, creada_por_id | |
| registro_acceso | usuario_id, accion, recurso, fecha | bitácora (RNF8): solo se inserta, no se edita |

Reglas que la base de datos debe reforzar (el código ya las valida, pero la BD es la última defensa):
- **Cupo:** al inscribir, contar inscripciones activas y comparar con `cupo_maximo` dentro de una transacción
  (hoy lo protege un candado en memoria, `InscripcionServicio.Candado`).
- Correos únicos, llaves foráneas y los índices únicos de la tabla anterior.
- La API se conecta con un usuario de BD con permisos mínimos (sin crear ni borrar tablas).
