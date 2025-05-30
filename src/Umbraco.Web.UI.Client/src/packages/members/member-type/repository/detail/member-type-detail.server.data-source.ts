import type { UmbMemberTypeDetailModel } from '../../types.js';
import { UMB_MEMBER_TYPE_ENTITY_TYPE } from '../../entity.js';
import { UmbId } from '@umbraco-cms/backoffice/id';
import type { UmbDetailDataSource } from '@umbraco-cms/backoffice/repository';
import type {
	CreateMemberTypeRequestModel,
	UpdateMemberTypeRequestModel,
} from '@umbraco-cms/backoffice/external/backend-api';
import { MemberTypeService } from '@umbraco-cms/backoffice/external/backend-api';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { tryExecute } from '@umbraco-cms/backoffice/resources';
import type { UmbPropertyContainerTypes } from '@umbraco-cms/backoffice/content-type';

/**
 * A data source for the Member Type that fetches data from the server
 * @class UmbMemberTypeDetailServerDataSource
 * @implements {UmbDetailDataSource<UmbMemberTypeDetailModel>}
 */
export class UmbMemberTypeDetailServerDataSource implements UmbDetailDataSource<UmbMemberTypeDetailModel> {
	#host: UmbControllerHost;

	/**
	 * Creates an instance of UmbMemberTypeDetailServerDataSource.
	 * @param {UmbControllerHost} host - The controller host for this controller to be appended to
	 * @memberof UmbMemberTypeDetailServerDataSource
	 */
	constructor(host: UmbControllerHost) {
		this.#host = host;
	}

	/**
	 * Creates a new Member Type scaffold
	 * @param {Partial<UmbMemberTypeDetailModel>} [preset] - Optional preset data to merge with the scaffold
	 * @returns { CreateMemberTypeRequestModel }
	 * @memberof UmbMemberTypeDetailServerDataSource
	 */
	async createScaffold(preset: Partial<UmbMemberTypeDetailModel> = {}) {
		const data: UmbMemberTypeDetailModel = {
			entityType: UMB_MEMBER_TYPE_ENTITY_TYPE,
			unique: UmbId.new(),
			name: '',
			alias: '',
			description: '',
			icon: 'icon-user',
			allowedAtRoot: false,
			variesByCulture: false,
			variesBySegment: false,
			isElement: false,
			properties: [],
			containers: [],
			allowedContentTypes: [],
			compositions: [],
			collection: null,
			...preset,
		};

		return { data };
	}

	/**
	 * Fetches a Member Type with the given id from the server
	 * @param {string} unique - The unique identifier of the Member Type to fetch
	 * @returns {*}
	 * @memberof UmbMemberTypeDetailServerDataSource
	 */
	async read(unique: string) {
		if (!unique) throw new Error('Unique is missing');

		const { data, error } = await tryExecute(this.#host, MemberTypeService.getMemberTypeById({ path: { id: unique } }));

		if (error || !data) {
			return { error };
		}

		// TODO: make data mapper to prevent errors
		const memberType: UmbMemberTypeDetailModel = {
			entityType: UMB_MEMBER_TYPE_ENTITY_TYPE,
			unique: data.id,
			name: data.name,
			alias: data.alias,
			description: data.description ?? '',
			icon: data.icon,
			allowedAtRoot: data.allowedAsRoot,
			variesByCulture: data.variesByCulture,
			variesBySegment: data.variesBySegment,
			isElement: data.isElement,
			properties: data.properties.map((property) => {
				return {
					id: property.id,
					unique: property.id,
					container: property.container ? { id: property.container.id } : null,
					sortOrder: property.sortOrder,
					alias: property.alias,
					name: property.name,
					description: property.description,
					dataType: { unique: property.dataType.id },
					variesByCulture: property.variesByCulture,
					variesBySegment: property.variesBySegment,
					validation: property.validation,
					appearance: property.appearance,
					visibility: property.visibility,
					isSensitive: property.isSensitive,
				};
			}),
			containers: data.containers.map((container) => {
				return {
					id: container.id,
					parent: container.parent ? { id: container.parent.id } : null,
					name: container.name ?? '',
					type: container.type as UmbPropertyContainerTypes, // TODO: check if the value is valid
					sortOrder: container.sortOrder,
				};
			}),
			allowedContentTypes: [],
			compositions: data.compositions.map((composition) => {
				return {
					contentType: { unique: composition.memberType.id },
					compositionType: composition.compositionType,
				};
			}),
			collection: data.collection ? { unique: data.collection.id } : null,
		};

		return { data: memberType };
	}

	/**
	 * Inserts a new Member Type on the server
	 * @param {UmbMemberTypeDetailModel} model - Member Type Model
	 * @returns {*}
	 * @memberof UmbMemberTypeDetailServerDataSource
	 */
	async create(model: UmbMemberTypeDetailModel) {
		if (!model) throw new Error('Member Type is missing');

		// TODO: make data mapper to prevent errors
		const body: CreateMemberTypeRequestModel = {
			alias: model.alias,
			name: model.name,
			description: model.description,
			icon: model.icon,
			allowedAsRoot: model.allowedAtRoot,
			variesByCulture: model.variesByCulture,
			variesBySegment: model.variesBySegment,
			isElement: model.isElement,
			properties: model.properties.map((property) => {
				return {
					id: property.id,
					container: property.container ? { id: property.container.id } : null,
					sortOrder: property.sortOrder,
					alias: property.alias,
					isSensitive: property.isSensitive ?? false,
					visibility: property.visibility ?? { memberCanEdit: false, memberCanView: false },
					name: property.name,
					description: property.description,
					dataType: { id: property.dataType.unique },
					variesByCulture: property.variesByCulture,
					variesBySegment: property.variesBySegment,
					validation: property.validation,
					appearance: property.appearance,
				};
			}),
			containers: model.containers,
			id: model.unique,
			compositions: model.compositions.map((composition) => {
				return {
					memberType: { id: composition.contentType.unique },
					compositionType: composition.compositionType,
				};
			}),
		};

		const { data, error } = await tryExecute(
			this.#host,
			MemberTypeService.postMemberType({
				body,
			}),
		);

		if (data && typeof data === 'string') {
			return this.read(data);
		}

		return { error };
	}

	/**
	 * Updates a MemberType on the server
	 * @param { UmbMemberTypeDetailModel } model - Member Type Model
	 * @returns {*}
	 * @memberof UmbMemberTypeDetailServerDataSource
	 */
	async update(model: UmbMemberTypeDetailModel) {
		if (!model.unique) throw new Error('Unique is missing');

		// TODO: make data mapper to prevent errors
		const body: UpdateMemberTypeRequestModel = {
			alias: model.alias,
			name: model.name,
			description: model.description,
			icon: model.icon,
			allowedAsRoot: model.allowedAtRoot,
			variesByCulture: model.variesByCulture,
			variesBySegment: model.variesBySegment,
			isElement: model.isElement,
			properties: model.properties.map((property) => {
				return {
					id: property.id,
					container: property.container ? { id: property.container.id } : null,
					sortOrder: property.sortOrder,
					isSensitive: property.isSensitive ?? false,
					visibility: property.visibility ?? { memberCanEdit: false, memberCanView: false },
					alias: property.alias,
					name: property.name,
					description: property.description,
					dataType: { id: property.dataType.unique },
					variesByCulture: property.variesByCulture,
					variesBySegment: property.variesBySegment,
					validation: property.validation,
					appearance: property.appearance,
				};
			}),
			containers: model.containers,
			compositions: model.compositions.map((composition) => {
				return {
					memberType: { id: composition.contentType.unique },
					compositionType: composition.compositionType,
				};
			}),
		};

		const { error } = await tryExecute(
			this.#host,
			MemberTypeService.putMemberTypeById({
				path: { id: model.unique },
				body,
			}),
		);

		if (!error) {
			return this.read(model.unique);
		}

		return { error };
	}

	/**
	 * Deletes a Member Type on the server
	 * @param {string} unique - The unique identifier of the Member Type to delete
	 * @returns {*}
	 * @memberof UmbMemberTypeDetailServerDataSource
	 */
	async delete(unique: string) {
		if (!unique) throw new Error('Unique is missing');

		return tryExecute(
			this.#host,
			MemberTypeService.deleteMemberTypeById({
				path: { id: unique },
			}),
		);
	}
}
