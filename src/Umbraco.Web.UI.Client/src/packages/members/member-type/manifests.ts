import { manifests as entityActionsManifests } from './entity-actions/manifests.js';
import { manifests as memberTypeRootManifests } from './member-type-root/manifests.js';
import { manifests as menuManifests } from './menu/manifests.js';
import { manifests as propertyTypeManifests } from './property-type/manifests.js';
import { manifests as repositoryManifests } from './repository/manifests.js';
import { manifests as searchManifests } from './search/manifests.js';
import { manifests as treeManifests } from './tree/manifests.js';
import { manifests as workspaceManifests } from './workspace/manifests.js';

import type { UmbExtensionManifestKind } from '@umbraco-cms/backoffice/extension-registry';

import './components/index.js';

export const manifests: Array<UmbExtensionManifest | UmbExtensionManifestKind> = [
	...entityActionsManifests,
	...memberTypeRootManifests,
	...menuManifests,
	...propertyTypeManifests,
	...repositoryManifests,
	...searchManifests,
	...treeManifests,
	...workspaceManifests,
];
