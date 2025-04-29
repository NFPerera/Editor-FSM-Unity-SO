# FSM Editor for Unity 🎮

Herramienta visual para diseñar **Máquinas de Estados Finitos (FSM)** directamente en el Editor de Unity. Ideal para comportamientos de IA.

## 🔥 Features Actuales

### ✨ **Editor Visual Intuitivo**
- **Drag & Drop** de nodos y conexiones.
- **Zoom** (con scroll) y **pan** para navegar por grafos grandes. //Actualmente bajo fix
- **Snap to Grid** para alinear nodos fácilmente.
- **Conexiones curvadas** con flechas y sombras para mejor legibilidad.

### 🏗 **Gestión de Estados**
- **Creación/eliminación** de estados con un click.
- **Renombrar estados** directamente desde el editor (con validación de nombres únicos).
- **Asignación de StateData** (ScriptableObjects) a cada nodo.
- **Creación automática de StateData** desde la interfaz.

### ⛓ **Conexiones Avanzadas**
- **Transiciones entre estados** con arrastrar y soltar.
- **Condiciones personalizables** (asignación de `StateCondition` ScriptableObjects).
- **Validación visual**: Conexiones en rojo/verde según validez.
- **Eliminar conexiones** desde el panel de condiciones.

### 🛠 **Integración con Código**
- **Vinculación directa** con `MonoBehaviour` y ScriptableObjects.
- **Soporte para comportamientos personalizados** (`MyState`).
- **Guardado automático** al cerrar la ventana.
- **Sistema de Undo/Redo** nativo de Unity.

### 📁 **Gestión de Assets**
- **Guardado/recuperación** del grafo completo.
- **Ruta personalizable** para guardar `StateData`.
- **Renombrado automático** de assets al editar nombres en el editor.

## 📦 Próximas Features (Work in Progress)
- [ ] **Shortcuts** para diseño rápido (Ctrl+C, Ctrl+V, etc.).
- [ ] **Mejor feedback visual** para errores y validaciones en ejecucion.

## 🚀 Cómo Usarlo
1. **Abre el editor**: `Tools > FSM Editor`.
2. **Asigna un State Machine** y una `StateCondition` por defecto.
3. **Crea estados** con el botón "Create New State".
4. **Conecta estados** haciendo click en "Add Connection" y arrastrando.
5. **Guarda cambios** con el botón "Save Changes".
