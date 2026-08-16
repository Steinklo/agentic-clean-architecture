# Todo

A reference implementation of a todo application, existing to demonstrate clean architecture and DDD patterns in C#. The domain is deliberately small; it is chosen to exercise an aggregate boundary, not to be feature-complete.

## Language

**TodoList**:
The aggregate root. A named collection of TodoItems, responsible for enforcing every invariant that spans more than one item.
_Avoid_: List, Board, Project, Category

**TodoItem**:
A single unit of work belonging to exactly one TodoList. It is a child entity — it has identity, but it can only be created or changed through its TodoList.
_Avoid_: Task, Todo, Item, Entry

**Complete**:
To mark a TodoItem as finished. Always performed through the owning TodoList, never on the item directly.
_Avoid_: Done, Finish, Check off, Close

**Archive**:
To retire a TodoList from active use. A TodoList cannot be archived while any of its TodoItems is incomplete — this is the invariant that justifies the aggregate boundary.
_Avoid_: Delete, Close, Remove, Deactivate

**Manifest**:
To make a TodoItem true in the physical world.
_Avoid_: Create, Do, Execute, Perform, Realize
