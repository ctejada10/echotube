   M S B A M L   `   `   `  ����I  EBicycleCounter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null/clr-namespace:BicycleCounterBicycleCounter  5   	       BicycleCounter.MainWindow   5   	   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   .localclr-namespace:BicycleCounter     5   	   -   5          � Title$  Bicycle Counter��5   	   $	��600��6!   $	��800��6.    � WindowStyle$
 None=�5	   	    � WindowStartupLocation$ CenterScreen=�6    � 
ResizeMode$ NoResize=�6A    � WindowState$ 	Maximized=�6W   .��5
      �� ���#222222._�5   
   (� .��5      �  � RowDefinitions 5      �� 5      $��* �6$    � ColumnDefinitions 5      �� 5      $��* �6'   $	��500��61   �� 5      $	��200 �6'   �� 5      $��* �6'   $	��500��61   .+�5      � $��0��6   $��0��6$    5      �� 5      $��* �6(   �� 5      $	��100 �6(   �� 5      $	��400 �6(   �� 5      $��* �6(   .+�5      �� $��BIKES TODAY��6   
�������$��Segoe UI)�6E   $��48(�6[   $��
UltraLight"�6i   $��Center=�6�   $��Center=�6�   $��1��6�   $��0��6�   �� 5      -   5         ��Name$ CounterLabel��5      $��0��62   
�������$��Segoe UI)�6Q   $	��256(�6g   $��Regular"�6v   $��0,0,0,50q�6�   $��Center=�6�   $��Center=�6�   $��2��6�   $��0��6�   5       �� 5"      $��1��6   $��	0,12,0,12q�6#   
�������$��0��6I   $��1��6V   � 5$      $��0��6   $��2��6$    5%      �� 5&      $��* �6(   �� 5'      $	��100 �6(   �� 5(      $	��220 �6(   �� 5)      $	��180 �6(   �� 5*      $��* �6(   .+�5-      �� $��	LAST BIKE��6   
�������$��Segoe UI)�6C   $��48(�6Y   $��
UltraLight"�6g   $��Center=�6   $��Center=�6�   $��1��6�   $��0��6�   � 5/      -   5         �Name$ 
RightArrow��5/      $��2��62   $��0��6?   $��Center=�6O   $��Center=�6l   $��0,80,0,0q�6�   	 � Points	 �    � 
�������$y�Visible=�6�   z�50      V� 51      .~�52   "   �� 
 �� Angle$

 90.0Q�62    �� CenterX$	 0.5Q�6?    �� CenterY$	 0.5Q�6M   �� 53   "    �� ScaleX$	 100Q�61    �� ScaleY$	 100Q�6>    �� CenterX$	 0.5Q�6K    �� CenterY$	 0.5Q�6Y   54      56      � 58      -   5        $ 	LeftArrow��58      $��2��61   $��0��6>   $��Center=�6N   $��Center=�6k   $��0,80,0,0q�6�   	 �    � 
�������$y�Hidden=�6�   z�59      V� 5:      .~�5;   "   �� $
 -90.0Q�62   $	 0.5Q�6@   $	 0.5Q�6N   �� 5<   "   $	 100Q�61   $	 100Q�6>   $	 0.5Q�6K   $	 0.5Q�6Y   5=      5?      �� 5A      -   5        $ 
SpeedLabel��5A      $��22 KM/H��60   
�������$��Segoe UI)�6U   $��48(�6k   $��Regular"�6y   $��Center=�6�   $	��Top=�6�   $��0,6,0,0q�6�   $��3��6�   $��0��6�   5B      5C      5D      5E      5F      