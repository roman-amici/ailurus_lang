# Tree Walker Interpreter

## Finish Tree Walker
 1. ~~Finish Function Declaration Evaluation~~
 2. ~~Add Call Semantics~~
    - ~~Add return semantics~~
 3. ~~Add Module Variable Declarations~~
 4. ~~Add Struct Declarations~~
   - ~~Add struct field reference syntax and semantics~~
 5. ~~Add Type alias Declarations~~
 6. Add Module resolution
   - 'this' and 'super' in module names
 7. ~~Pointer semantics (without arithmetic)~~.
 8. ~~Mutability semantics for pointers.~~
 9. ~~Array syntax and semantics.~~
 10. ~~Array element assignment.~~
 11. ~~Mutability semantics + syntax for arrays.~~
 12. ~~String and char types~~
 13. ~~Foreach loop on arrays and strings.~~
 14. ~~Heap allocation (new, free)~~
 15. Variants
 16. ~~Address of array elements~~
  - ~~Address of array elements in foreach loop~~
 17. ~~Tuples~~
 18. Match Statement
 19. Uninitialized variable check / code flow analysis
 20. Constant Expressions
 21. Function pointers as static types
 21. Import variable renaming

## Front End Cleanup
1. Synchronization
2. Split Resolver into multiple passes
 - Type Checking
 - Variable Resolution
 - Static Analysis
3. Standardize Error Handling and warnings
4. Spans for error handling (rather than just columns)
5. Different IR for parser output and resolver output
 - Or at least split the properties better
6. Remove switching on node enum. Do it all with pattern matching.
7. Lvalue / Rvalue distinction instead of multiple assignment expressions for each lhs.

## Standard Library
 1. Build minimal C-like standard library
 2. Add Integration tests for what I have so that VM can be regression tested.

## VM
 1. Setup memory regions and stack machine
 2. Implement existing features in VM and pass regression tests
 3. Add pointer arithmetic and casting
 4. Add raw array manipulation
 5. Write minimal standard library

## Compile to source
 1. Compile to asm first?
 2. Compile to llvm?

## New Features
 1. Basic generics
 2. Lambdas
 3. Question Mark Operator
 4. Try+Finally or Defer
 5. Some sort of conditional compilation
 6. Proc macros?

## Infrastructure
 1. VSCode Syntax highlighting
 2. VSCode language server
 3. Build system and package manager
