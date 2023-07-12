https://mods.vintagestory.at/attributer
<hr />
<h2 style="text-align: center;">A multi-class patch that makes lets you set attributes per item dynamically.</h2>
<h4 style="text-align: center;">Basically wherever the code says "get parent object attribute" it instead "get object attribute if exists first".</h4>
<h4 style="text-align: center;"><strong>Also adds a few new attributes that can be applied to items to modify their functionality.</strong></h4>
<hr />
<p><strong>Requirements:<br /></strong></p>
<ul>
<li>None.</li>
</ul>
<p><strong>Incompatibilities:</strong></p>
<ul>
<li>Bullseye (Compatibility Planned).</li>
<li>Other mods which significantly overwrite base item types.</li>
</ul>
<p><strong>Mods requiring this:</strong></p>
<ul>
<li><a title="RPG Item Rarity" href="https://mods.vintagestory.at/rpgitemrarity">RPG Item Rarity</a></li>
</ul>
<p><strong>New Attributes:</strong></p>
<ul>
<li>maxdurability.</li>
</ul>
<p><strong>Supported Attributes:<br /></strong></p>
<ul>
<li>defaultProtLoss</li>
<li>protectionModifiers
<ul>
<li>protectionTier</li>
<li>highDamageTierResistant</li>
<li>flatDamageReduction</li>
<li>relativeProtection</li>
</ul>
</li>
<li>statModifiers
<ul>
<li>healingeffectiveness</li>
<li>hungerrate</li>
<li>rangedWeaponsAcc</li>
<li>rangedWeaponsSpeed</li>
<li>walkSpeed</li>
</ul>
</li>
<li>damage</li>
<li>attackpower</li>
<li>attackrange</li>
<li>miningspeed</li>
<li>miningtier</li>
<li>health</li>
<li>clothescategory</li>
<li>breakChanceOnImpact</li>
<li>requiresAnvilTier</li>
<li>workableTemperature</li>
</ul>
<p>- Please note, some attributes are enabled by the api by default and do not need this mod.<br />- Search the <a href="https://github.com/anegostudios/vsapi/" target="_blank" rel="noopener">official API</a> and <a href="https://github.com/anegostudios/vssurvivalmod">official survival mod</a> github repositorys to check.</p>
<p><strong>Attribute Exceptions:<br /></strong></p>
<ul>
<li>perTierFlatDamageReductionLoss</li>
<li>perTierRelativeProtectionLoss</li>
</ul>
<p>These aren't mirrored as other attributes are, as arrays aren't supported by the API.<br />Instead, they should be TreeAttributes, with the float values of "0" and "1" placed into them.<br />This is a workaround until the API updates.</p>
<p><strong>How to use (For developers):</strong></p>
<ol>
<li>Set an attribute on an item.<span style="background-color: #2b3e50; color: #ffffff;"><br /></span>
<ol>
<li>Example (Set an items attack power):<br /><br />
<ol>
<li><span style="background-color: #2b3e50; color: #ffffff;">itemstack.Attributes.SetFloat("attackpower", 12f);<br /><br /></span></li>
</ol>
</li>
<li>Example (Set an items flat damage reduction):<br /><span style="background-color: #2b3e50; color: #ffffff;"><br /></span>
<ol>
<li><span style="background-color: #2b3e50; color: #ffffff;">itemstack.Attributes.GetTreeAttribute("protectionModifiers").SetFloat("flatDamageReduction", 8f);<br /><br /></span></li>
</ol>
</li>
</ol>
</li>
<li>That's it. It should work if the attribute is supported.</li>
</ol>
<p><strong>How to use (Standalone):</strong></p>
<ol>
<li>Use an command that supports or sets attributes.
<ol>
<li>Example (Set the weapon in your hand's attack power.):<br /><br />
<ol>
<li><span style="color: #ecf0f1; background-color: #2b3e50;">/debug heldstattr attackpower float 1000000<br /><br /></span></li>
</ol>
</li>
</ol>
</li>
<li>That should do it.</li>
</ol>
<p><span style="text-decoration: underline;">Please note you must mirror attributes exactly, including trees, to how the parent item is.</span><strong><br /></strong></p>
