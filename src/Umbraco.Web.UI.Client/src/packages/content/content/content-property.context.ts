import { UMB_CONTENT_PROPERTY_CONTEXT } from './content-property.context-token.js';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbContextBase } from '@umbraco-cms/backoffice/class-api';
import { UmbObjectState } from '@umbraco-cms/backoffice/observable-api';
import type { UmbPropertyTypeModel } from '@umbraco-cms/backoffice/content-type';

export class UmbContentPropertyContext extends UmbContextBase {
	#dataType = new UmbObjectState<UmbPropertyTypeModel['dataType'] | undefined>(undefined);
	dataType = this.#dataType.asObservable();

	constructor(host: UmbControllerHost) {
		super(host, UMB_CONTENT_PROPERTY_CONTEXT);
	}

	setDataType(dataType: UmbPropertyTypeModel['dataType'] | undefined) {
		this.#dataType.setValue(dataType);
	}
}
