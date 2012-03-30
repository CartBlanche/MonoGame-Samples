BackgroundThreadSample
======================

This sample demonstrates the loading of assets in a background thread.

Sample originally created by CircleOf14 and modified by Kenneth Pouncey to create new textures to be added
to the game components dynamically in the background.

Of special interest look at the following methods:

CreateBackgroundThread () - Creates a thread to be executed in the background and starts it.
BackgroundWorkerThread () - Worker thread that actually does the work of creating the new asset and adding

Make sure to read the comments in these two methods.

