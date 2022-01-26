---
# An instance of the About widget.
# Documentation: https://wowchemy.com/docs/page-builder/
widget: blank

# Activate this widget? true/false
active: true

# This file represents a page section.
headless: true

# Order that this section appears on the page.
weight: 40

title: ⏱ Performance

design:
  columns: '2'
---

Creating thousands of roads in a landscape made of millions of cells takes **less than a minute** for each time step with our module, when using an average CPU for the time (Intel i7 CPU with 4 cores working at 2.60GHz).

Using the woodflux algorithm is also really quick, often flushing the wood half the time that was needed to create the roads for the time step.

This is due to optimizations using two open-source C# Nuget packages (see Acknowledgments) that greatly improved the running time of the module.