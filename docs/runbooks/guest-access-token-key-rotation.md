# Runbook: Rotación de llaves de invitado

## Propósito
Rotar la llave de derivación usada para los tokens de acceso de invitados sin invalidar enlaces existentes.

## Prerrequisitos
- Acceso al administrador de secretos del entorno (vault, key management service o archivos de configuración seguros).
- Acceso de lectura a la base de datos para consultar enlaces activos.
- Permiso para desplegar la aplicación.

---

## Alta de una llave nueva

### 1. Generar secreto seguro
Generar al menos 64 caracteres aleatorios:
openssl rand -base64 48

### 2. Agregar a configuración
En `appsettings.Production.json` (o el mecanismo de secretos del entorno):
```json
{
  "GuestAccessTokens": {
    "ActiveKeyId": "2026-08",
    "Keys": {
      "2026-08": "<nuevo-secreto>",
      "2026-01": "<secreto-anterior>"
    }
  }
}
```

En variables de entorno, usar exactamente:

```text
GuestAccessTokens__ActiveKeyId=2026-08
GuestAccessTokens__Keys__2026-08=<nuevo-secreto>
GuestAccessTokens__Keys__2026-01=<secreto-anterior>
```

### 3. Validar llave anterior
Mantener todas las llaves anteriores en el diccionario `Keys`. Cada enlace existente conserva su `DerivationKeyId` y debe poder reconstruirse con la llave correspondiente.

### 4. Cambiar ActiveKeyId
Actualizar `ActiveKeyId` al identificador de la nueva llave.

### 5. Desplegar
Desplegar la aplicación. La aplicación fallará al iniciar si `ActiveKeyId` no existe en `Keys`.

### 6. Verificar
1. Crear un enlace de prueba. Debe usar la nueva `ActiveKeyId`.
2. Abrir un enlace existente creado con la llave anterior. Debe funcionar.
3. Validar que `DerivationKeyId` en la tabla `guest_access_links` coincide con la llave usada.

---

## Retiro de una llave anterior

### 1. Consultar enlaces activos
```sql
SELECT derivation_key_id, COUNT(*) AS active_links
FROM guest_access_links
WHERE status = 'Active'
GROUP BY derivation_key_id;
```

### 2. Verificar dependencias
**No retirar una llave mientras existan enlaces activos con ese `DerivationKeyId`.**

Si existen enlaces activos:
- Regenerar los enlaces (esto les asignará la nueva `ActiveKeyId`).
- O esperar a que expiren naturalmente.
- O revocarlos si ya no son necesarios.

### 3. Confirmar
```sql
SELECT COUNT(*) FROM guest_access_links
WHERE status = 'Active' AND derivation_key_id = '<key-a-retirar>';
```
Debe retornar 0.

### 4. Remover de configuración
Eliminar la entrada del diccionario `Keys`.

### 5. Desplegar
Si la llave retirada no era la activa ni tiene enlaces dependientes, el despliegue será exitoso.

### 6. Auditar
Registrar en bitácora de operaciones:
- Fecha de retiro.
- Identificador de llave retirada.
- Responsable.
- Confirmación de cero enlaces activos.
**No registrar el valor del secreto.**

---

## Escenarios de recuperación

### Llave activa perdida
1. Generar una nueva llave inmediatamente.
2. Configurarla como `ActiveKeyId`.
3. Todos los enlaces nuevos usarán la nueva llave.
4. Los enlaces existentes con la llave perdida **no podrán validarse**. Deberán regenerarse.
5. Notificar a los organizadores de eventos activos.

### Llave histórica perdida
1. Los enlaces creados con esa llave **no podrán abrirse**.
2. Identificar los `guest_access_links` afectados:
   ```sql
   SELECT id, invitation_group_id FROM guest_access_links
   WHERE derivation_key_id = '<key-perdida>' AND status = 'Active';
   ```
3. Regenerar cada enlace afectado.
4. Notificar a los contactos de cada grupo.

### Llave incorrecta (typo)
1. Corregir el valor en configuración.
2. Si la llave ya se usó para crear enlaces, esos enlaces contendrán un hash que no corresponde.
3. Identificar enlaces creados durante la ventana de error:
   ```sql
   SELECT id, created_at FROM guest_access_links
   WHERE derivation_key_id = '<key-id-incorrecta>';
   ```
4. Regenerar los enlaces afectados.

### La aplicación no inicia
Si la aplicación falla con `InvalidOperationException: La llave de derivación 'X' no existe`:
1. Verificar que `ActiveKeyId` existe en el diccionario `Keys`.
2. Verificar que cada `DerivationKeyId` presente en `guest_access_links` activos existe en `Keys`.
3. Agregar la llave faltante o regenerar/revocar los enlaces huérfanos.

### Revertir un despliegue
Si se revierte a una versión anterior:
1. La versión anterior debe tener configuradas las mismas llaves (o al menos las requeridas por los enlaces activos).
2. Si la versión anterior no soporta `DerivationKeyId` (formato antiguo), los enlaces nuevos no funcionarán.
3. Mantener sincronizadas las configuraciones entre versiones.

---

## Herramienta administrativa

Existe una herramienta de diagnóstico que muestra conteos de enlaces por `KeyId`:

```sql
SELECT derivation_key_id, status, COUNT(*)
FROM guest_access_links
GROUP BY derivation_key_id, status;
```

**Esta herramienta nunca muestra valores de llaves.**

Las llaves viven únicamente en el secret manager o en la configuración segura.
PostgreSQL no contiene una entidad ni una tabla `guest_access_token_keys`; los
conteos de dependencias se obtienen de `guest_access_links`.
