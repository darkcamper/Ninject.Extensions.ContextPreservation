Version 3.2.0
-------------
Bugfix: Fixed ContextPresevingGet for closed generic types (#3)

Version 3.0.1
-------------
Bugfix: Fixed incompatibility with the factory extension, factories can now be resolved directly.

Version 3.0.0
-------------
Added:  Support for Ninject.Extensions.Factory
Change: Removed NoWeb builds because they are not needed anymore
Change: Removed BindInterfaceToBinding use the new core functionality to bind multiple interfaces instead. (Bind<IA, IB>())